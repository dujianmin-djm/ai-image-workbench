const { app, BrowserWindow, dialog } = require("electron");
const { spawn } = require("child_process");
const path = require("path");
const http = require("http");

let mainWindow = null;
let backendProcess = null;

const isPackaged = app.isPackaged;

// 前端、后端资源目录
const frontendDir = isPackaged
  ? path.join(process.resourcesPath, "frontend-dist")
  : path.join(__dirname, "frontend-dist");

const backendDir = isPackaged
  ? path.join(process.resourcesPath, "backend-dist")
  : path.join(__dirname, "backend-dist");

const frontendIndex = path.join(frontendDir, "index.html");
const backendExe = path.join(backendDir, "AI.Image.HttpApi.Host.exe");

const BACKEND_PORT = 5008;
const BACKEND_URL = `http://127.0.0.1:${BACKEND_PORT}`;

// 启动后端
function startBackend() {
  return new Promise((resolve, reject) => {
    console.log("isPackaged:", isPackaged);
    console.log("frontendDir:", frontendDir);
    console.log("frontendIndex:", frontendIndex);
    console.log("backendDir:", backendDir);
    console.log("backendExe:", backendExe);

    backendProcess = spawn(backendExe, [], {
      cwd: backendDir,
      env: {
        ...process.env,
        ASPNETCORE_URLS: BACKEND_URL,
        ASPNETCORE_ENVIRONMENT: "Production"
      },
      stdio: "pipe",
      windowsHide: true
    });

    backendProcess.stdout?.on("data", (data) => {
      console.log(`[Backend] ${data.toString()}`);
    });

    backendProcess.stderr?.on("data", (data) => {
      console.error(`[Backend Error] ${data.toString()}`);
    });

    backendProcess.on("error", (err) => {
      console.error("Backend process error:", err);
      reject(err);
    });

    backendProcess.on("close", (code) => {
      console.log(`Backend exited with code ${code}`);
    });

    resolve();
  });
}

// 等待后端就绪
function waitForBackend(retries = 30, interval = 1000) {
  return new Promise((resolve, reject) => {
    const check = (remaining) => {
      const req = http.get(
        `${BACKEND_URL}/dapi/book/query?current=1&size=1`,
        (res) => {
          console.log("Backend check status:", res.statusCode);

          if (res.statusCode === 200) {
            resolve();
          } else if (remaining > 0) {
            setTimeout(() => check(remaining - 1), interval);
          } else {
            reject(
              new Error(`后端未就绪，状态码：${res.statusCode}`)
            );
          }
        }
      );

      req.on("error", (err) => {
        if (remaining > 0) {
          setTimeout(() => check(remaining - 1), interval);
        } else {
          reject(new Error(`后端启动失败：${err.message}`));
        }
      });
    };

    check(retries);
  });
}

// 创建主窗口
function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    title: "AI 图片筛选与评审工作台",
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true
    }
  });

  console.log("Loading frontend:", frontendIndex);

  mainWindow.loadFile(frontendIndex);

  // 调试时可打开
  // mainWindow.webContents.openDevTools();

  mainWindow.webContents.on("did-fail-load", (_, code, desc, url) => {
    console.error("did-fail-load:", code, desc, url);
    dialog.showErrorBox(
      "前端页面加载失败",
      `code: ${code}\ndesc: ${desc}\nurl: ${url}`
    );
  });

  mainWindow.webContents.on("console-message", (_, level, message) => {
    console.log(`[Renderer:${level}] ${message}`);
  });

  mainWindow.on("closed", () => {
    mainWindow = null;
  });
}

// 关闭后端
function stopBackend() {
  if (backendProcess) {
    try {
      backendProcess.kill();
    } catch (e) {
      console.error("停止后端失败:", e);
    }
    backendProcess = null;
  }
}

app.whenReady().then(async () => {
  try {
    await startBackend();
    await waitForBackend();
    createWindow();
  } catch (e) {
    console.error("应用启动失败:", e);
    dialog.showErrorBox("应用启动失败", e.message || String(e));
    app.quit();
  }

  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on("window-all-closed", () => {
  stopBackend();
  if (process.platform !== "darwin") {
    app.quit();
  }
});

app.on("before-quit", () => {
  stopBackend();
});