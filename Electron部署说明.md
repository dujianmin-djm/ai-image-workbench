# 使用 Electron 打包为桌面应用

本文档说明如何将当前的 Vue 3 前端 + .NET 后端项目打包为 Electron 桌面应用。

## 整体思路

Electron 本质是一个内嵌 Chromium 浏览器的桌面容器。对于本项目，有两种架构方案：

### 方案 A：Electron 仅包装前端，后端独立运行（推荐）

```
Electron 进程
├── 渲染进程 (BrowserWindow) → 加载 Vue 前端页面
└── 主进程 (main.js) → 启动时自动拉起 .NET 后端子进程

用户双击 .exe → Electron 启动 → 自动启动 dotnet 后端 → 加载前端界面
```

**优点：** 改动最小，前后端代码几乎不需要修改  
**缺点：** 需要一起打包 .NET 运行时或要求用户安装

### 方案 B：前端纯静态 + Electron 内嵌后端

与方案 A 类似，但前端构建为静态文件，由 .NET 后端直接托管静态资源（不再需要 Vite 开发服务器）。

**本文以方案 A 为主进行说明。**

---

## 步骤 1：发布 .NET 后端为独立可执行文件

```bash
cd backend
dotnet publish src/AI.Image.HttpApi.Host/AI.Image.HttpApi.Host.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -o ../electron-app/backend-dist
```

参数说明：

- `-r win-x64`：目标平台为 Windows 64 位
- `--self-contained true`：包含 .NET 运行时，用户无需额外安装
- `-o`：输出到 electron 项目中的一个子目录

发布后 `electron-app/backend-dist/` 中会有一个 `AI.Image.HttpApi.Host.exe`，可独立运行。

---

## 步骤 2：构建 Vue 前端

```bash
cd frontend
npm run build
```

构建产物在 `frontend/dist/` 目录中。将其复制到 Electron 项目中：

```bash
xcopy /E /I frontend\dist electron-app\frontend-dist
```

---

## 步骤 3：创建 Electron 项目

```bash
mkdir electron-app
cd electron-app
npm init -y
npm install electron --save-dev
npm install electron-builder --save-dev
```

---

## 步骤 4：编写 Electron 主进程

创建 `electron-app/main.js`：

```javascript
const { app, BrowserWindow } = require("electron");
const { spawn } = require("child_process");
const path = require("path");
const http = require("http");

let mainWindow;
let backendProcess;

// 后端可执行文件路径
const backendExe = path.join(
  __dirname,
  "backend-dist",
  "AI.Image.HttpApi.Host.exe",
);
const BACKEND_PORT = 5008;
const BACKEND_URL = `http://localhost:${BACKEND_PORT}`;

function startBackend() {
  backendProcess = spawn(backendExe, [], {
    cwd: path.join(__dirname, "backend-dist"),
    env: {
      ...process.env,
      ASPNETCORE_URLS: BACKEND_URL,
      ASPNETCORE_ENVIRONMENT: "Production",
    },
    stdio: "pipe",
  });

  backendProcess.stdout.on("data", (data) => {
    console.log(`[Backend] ${data}`);
  });

  backendProcess.stderr.on("data", (data) => {
    console.error(`[Backend Error] ${data}`);
  });

  backendProcess.on("close", (code) => {
    console.log(`Backend exited with code ${code}`);
  });
}

// 等待后端就绪
function waitForBackend(retries = 30, interval = 1000) {
  return new Promise((resolve, reject) => {
    const check = (remaining) => {
      http
        .get(`${BACKEND_URL}/api/abp/application-configuration`, (res) => {
          if (res.statusCode === 200) {
            resolve();
          } else if (remaining > 0) {
            setTimeout(() => check(remaining - 1), interval);
          } else {
            reject(new Error("Backend did not start in time"));
          }
        })
        .on("error", () => {
          if (remaining > 0) {
            setTimeout(() => check(remaining - 1), interval);
          } else {
            reject(new Error("Backend did not start in time"));
          }
        });
    };
    check(retries);
  });
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    title: "AI 图片筛选与评审工作台",
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
    },
  });

  // 加载前端静态文件
  // 方式 1：加载本地构建产物
  mainWindow.loadFile(path.join(__dirname, "frontend-dist", "index.html"));

  // 方式 2（开发时）：加载 Vite 开发服务器
  // mainWindow.loadURL('http://localhost:5007');

  mainWindow.on("closed", () => {
    mainWindow = null;
  });
}

app.on("ready", async () => {
  // 启动后端
  startBackend();

  try {
    // 等待后端就绪
    await waitForBackend();
  } catch (e) {
    console.error("Failed to start backend:", e.message);
  }

  // 创建窗口
  createWindow();
});

app.on("window-all-closed", () => {
  // 关闭后端进程
  if (backendProcess) {
    backendProcess.kill();
  }
  app.quit();
});

app.on("before-quit", () => {
  if (backendProcess) {
    backendProcess.kill();
  }
});
```

---

## 步骤 5：调整前端 API 请求地址

由于 Electron 加载的是本地文件（`file://` 协议），Vite 代理不再生效。需要修改前端的 API 基础地址。

修改 `frontend/src/api/request.ts`：

```typescript
// 判断是否在 Electron 中运行
const isElectron = navigator.userAgent.includes("Electron");

const http = axios.create({
  // Electron 环境中直接请求后端地址；浏览器环境走 Vite 代理
  baseURL: isElectron ? "http://localhost:5008/dapi" : "/dapi",
  timeout: 30000,
});
```

同理，图片 URL 的拼接逻辑（`imageApi.getFileUrl`）在 Electron 中也需要使用完整的后端地址。

---

## 步骤 6：配置 package.json

编辑 `electron-app/package.json`：

```json
{
  "name": "ai-image-workbench",
  "version": "1.0.0",
  "description": "AI 图片筛选与评审工作台",
  "main": "main.js",
  "scripts": {
    "start": "electron .",
    "build": "electron-builder"
  },
  "build": {
    "appId": "com.aiimage.workbench",
    "productName": "AI图片工作台",
    "directories": {
      "output": "dist-electron"
    },
    "files": ["main.js", "frontend-dist/**/*", "backend-dist/**/*"],
    "extraResources": [
      {
        "from": "backend-dist",
        "to": "backend-dist",
        "filter": ["**/*"]
      }
    ],
    "win": {
      "target": "nsis",
      "icon": "icon.ico"
    },
    "nsis": {
      "oneClick": false,
      "allowToChangeInstallationDirectory": true,
      "installerIcon": "icon.ico",
      "uninstallerIcon": "icon.ico",
      "createDesktopShortcut": true,
      "shortcutName": "AI图片工作台"
    }
  },
  "devDependencies": {
    "electron": "^35.0.0",
    "electron-builder": "^26.0.0"
  }
}
```

> **注意：** 使用 `extraResources` 时，后端路径需要在 `main.js` 中调整为 `process.resourcesPath`：
>
> ```javascript
> const backendExe = app.isPackaged
>   ? path.join(
>       process.resourcesPath,
>       "backend-dist",
>       "AI.Image.HttpApi.Host.exe",
>     )
>   : path.join(__dirname, "backend-dist", "AI.Image.HttpApi.Host.exe");
> ```

---

## 步骤 7：打包

```bash
cd electron-app

# 开发测试
npm start

# 打包为安装程序
npm run build
```

打包完成后在 `electron-app/dist-electron/` 目录中会生成 Windows 安装程序（.exe）。

---

## 完整流程汇总

```bash
# 1. 发布后端
cd backend
dotnet publish src/AI.Image.HttpApi.Host/AI.Image.HttpApi.Host.csproj -c Release -r win-x64 --self-contained true -o ../electron-app/backend-dist

# 2. 构建前端
cd ../frontend
npm run build
xcopy /E /I dist ..\electron-app\frontend-dist

# 3. 初始化 Electron 项目（首次）
cd ../electron-app
npm init -y
npm install electron electron-builder --save-dev
# 创建 main.js 和配置 package.json（见上文）

# 4. 测试运行
npm start

# 5. 打包为安装程序
npm run build
```

---

## 注意事项

1. **数据库文件位置**：打包后 `Image.db` 会在 `backend-dist/` 目录中生成。如果使用 `extraResources`，实际路径为 `%APPDATA%/../Local/Programs/AI图片工作台/resources/backend-dist/Image.db`。

2. **uploads 目录**：图片上传存储路径同理，需确保后端的工作目录（`cwd`）设置正确。

3. **杀毒软件**：部分杀毒软件可能因 Electron 启动子进程而告警，可在打包时进行代码签名避免。

4. **自动 CORS**：Electron 加载 `file://` 页面请求 `http://localhost:5008` 时属于跨域，需确保后端 CORS 策略允许。ABP 项目在 `HttpApiHostModule` 中已配置 CORS，添加 `file://` 或使用 `AllowAnyOrigin()`。

5. **包体积**：`--self-contained` 会包含 .NET 运行时（约 80-100MB）。如果确定用户已安装 .NET 10，可改为 `--self-contained false` 减小体积。

6. **替代方案**：如果觉得 Electron 太重，也可以考虑：
   - **Tauri**：基于系统 WebView，包体积更小（约 5-10MB），但需要 Rust 工具链
   - **WebView2**：微软的方案，仅限 Windows，利用系统自带的 Edge WebView
