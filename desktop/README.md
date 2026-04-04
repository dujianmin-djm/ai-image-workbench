# AI 图片筛选与评审工作台 - Desktop 打包说明

本目录用于将现有的前后端分离项目封装为 Windows 桌面应用。

桌面应用基于 Electron，运行方式为：

1. Electron 启动时先拉起本地 .NET 后端服务。
2. 后端监听本地端口，当前配置为 127.0.0.1:5008。
3. Electron 窗口加载前端静态页面。
4. 前端通过 HTTP 请求本地后端接口完成业务功能。

## 一、目录结构说明

当前 desktop 目录主要结构如下：

```text
desktop/
├── package.json
├── main.js
├── icon.ico                 # 可选，安装包/应用图标
├── frontend-dist/           # 前端构建产物（由 frontend/dist 拷贝而来）
├── backend-dist/            # 后端发布产物（由 dotnet publish 输出）
├── dist-electron/           # Electron 构建输出目录
└── README.md
```

## 二、各文件/目录作用说明

### 1. package.json

Electron 工程配置文件，主要作用：

- 定义 Electron 应用入口 main.js。
- 定义 npm 脚本。
- 定义 electron-builder 打包配置。

当前脚本如下：

```json
{
	"start": "electron .",
	"build": "electron-builder --win nsis",
	"build:dir": "electron-builder --win dir",
	"build:nsis": "electron-builder --win nsis"
}
```

当前 electron-builder 配置已包含：

- 应用名称与安装包命名。
- 输出目录 dist-electron。
- extraResources，将 frontend-dist 和 backend-dist 打进安装产物。
- NSIS 安装器配置。
- 桌面快捷方式与开始菜单快捷方式。

### 2. main.js

Electron 主进程入口文件，作用：

- 启动 backend-dist/AI.Image.HttpApi.Host.exe。
- 检测后端是否就绪。
- 创建桌面主窗口。
- 加载 frontend-dist/index.html。
- 在应用退出时关闭后端进程。

这是桌面端的核心启动逻辑。

当前实现细节：

- 开发态从 desktop/frontend-dist 和 desktop/backend-dist 读取资源。
- 打包态从 process.resourcesPath 下读取资源。
- 后端启动地址固定为 http://127.0.0.1:5008。
- 当前后端就绪检查接口使用 dapi/book/query?current=1&size=1。

### 3. icon.ico

Windows 应用图标文件。

用于：

- 应用 exe 图标。
- 安装程序图标。
- 卸载程序图标。
- 桌面快捷方式图标。

如果图标文件不存在，而 package.json 中又配置了 icon 相关字段，构建可能失败。当前做法二选一：

1. 补充一个有效的 icon.ico。
2. 暂时删除 package.json 中 win.icon 与 nsis 下的 icon 配置。

### 4. frontend-dist/

前端构建结果目录，由 frontend 项目执行 npm run build 后生成，再复制到本目录。

它通常包含：

```text
frontend-dist/
├── index.html
└── assets/
```

Electron 实际加载的是这里的 index.html。

### 5. backend-dist/

后端发布结果目录，由 .NET 项目执行 dotnet publish 后输出到本目录。

它通常包含：

- AI.Image.HttpApi.Host.exe
- 一系列 .dll
- appsettings.json
- 运行所需其他文件

Electron 会启动这个目录中的 exe。

### 6. dist-electron/

Electron 打包输出目录。

常见内容：

- win-unpacked：目录版可执行程序。
- 安装包 exe：正式安装包。

## 三、运行原理说明

桌面应用启动流程如下：

1. 运行 Electron 应用。
2. main.js 启动 backend-dist/AI.Image.HttpApi.Host.exe。
3. 后端监听：

```text
http://127.0.0.1:5008
```

4. Electron 轮询后端接口，确认服务就绪。
5. Electron 加载：

```text
frontend-dist/index.html
```

6. 前端访问后端接口，例如：

```text
http://127.0.0.1:5008/dapi/...
```

当前前端已做了桌面化适配：

- Vite 配置 base 为 ./，避免静态资源在 file 协议下丢失。
- Vue Router 使用 createWebHashHistory，避免桌面环境刷新时路由失效。
- 图片与接口访问都改为显式后端地址，不依赖 Vite 开发代理。

## 四、前置要求

在进行本地测试或正式打包前，需要准备以下环境：

### 1. Node.js

建议安装 LTS 版本，例如 Node.js 18 或 20。

检查命令：

```powershell
node -v
npm -v
```

### 2. .NET SDK

用于发布后端。

检查命令：

```powershell
dotnet --version
```

### 3. Electron 依赖安装

首次进入 desktop 目录需安装依赖：

```powershell
cd D:\作业\ai-image-workbench\desktop
npm install
```

## 五、前端构建与拷贝

### 1. 进入前端目录

```powershell
cd D:\作业\ai-image-workbench\frontend
```

### 2. 设置桌面版构建环境变量

当前前端请求层使用以下环境变量：

- VITE_API_BASE_URL：接口基础地址，例如 http://127.0.0.1:5008/dapi。
- VITE_BACKEND_ORIGIN：静态图片基础地址，例如 http://127.0.0.1:5008。

PowerShell 下可这样设置：

```powershell
$env:VITE_API_BASE_URL = "http://127.0.0.1:5008/dapi"
$env:VITE_BACKEND_ORIGIN = "http://127.0.0.1:5008"
```

### 3. 构建前端

```powershell
npm run build
```

### 4. 将构建产物复制到 desktop

```powershell
xcopy /E /I /Y dist ..\desktop\frontend-dist
```

## 六、后端发布

### 1. 进入 backend 目录

```powershell
cd D:\作业\ai-image-workbench\backend
```

### 2. 发布后端到 desktop/backend-dist

```powershell
dotnet publish src/AI.Image.HttpApi.Host/AI.Image.HttpApi.Host.csproj -c Release -r win-x64 --self-contained true -o ../desktop/backend-dist
```

参数说明：

- -c Release：发布模式。
- -r win-x64：面向 Windows 64 位。
- --self-contained true：自带 .NET 运行时，目标机器无需另外安装 .NET。
- -o ../desktop/backend-dist：输出到 desktop 目录中。

## 七、本地开发态运行测试

用于验证 Electron 能否正确启动前后端。

### 1. 确保前端和后端产物已准备好

需存在：

- desktop/frontend-dist/index.html
- desktop/backend-dist/AI.Image.HttpApi.Host.exe

### 2. 运行 Electron

```powershell
cd D:\作业\ai-image-workbench\desktop
$env:NODE_OPTIONS = ""
npm start
```

### 3. 预期结果

- Electron 窗口正常打开。
- 后端自动启动。
- 页面可正常访问接口。
- 页面数据加载正常。
- 导出等功能可正常调用本地后端。

## 八、目录版打包测试

目录版适合先验证打包后是否能独立运行。

### 1. 执行构建

```powershell
cd D:\作业\ai-image-workbench\desktop
npm run build:dir
```

### 2. 构建输出位置

```text
desktop/dist-electron/win-unpacked/
```

### 3. 测试运行

双击：

```text
AI 图片筛选与评审工作台.exe
```

或命令行运行：

```powershell
cd D:\作业\ai-image-workbench\desktop\dist-electron\win-unpacked
.\AI 图片筛选与评审工作台.exe
```

### 4. 需要重点验证的内容

- 窗口能否正常打开。
- 后端是否能自动启动。
- 首页是否能正常加载数据。
- 项目列表、详情、图片详情是否可正常进入。
- 对比评审是否正常。
- 导出功能是否正常。
- 关闭应用后后端进程是否退出。

## 九、正式安装包发布

当目录版验证通过后，再生成正式安装包。

### 1. 执行打包

```powershell
cd D:\作业\ai-image-workbench\desktop
npm run build:nsis
```

如果 build 脚本默认指向 nsis，也可以直接执行：

```powershell
npm run build
```

### 2. 输出位置

通常在：

```text
desktop/dist-electron/
```

生成类似文件：

```text
AI 图片筛选与评审工作台-Installer-1.0.0.exe
```

### 3. 安装测试

在本机或测试机器执行安装，检查：

- 安装流程是否正常。
- 是否创建桌面快捷方式。
- 启动后应用是否可正常运行。
- 后端是否能自动启动。
- 导入、导出、上传、评审等核心功能是否正常。
- 卸载是否正常。

## 十、推荐的完整发布流程

每次准备发布桌面应用时，建议按以下顺序操作：

### 步骤 1：构建前端

```powershell
cd D:\作业\ai-image-workbench\frontend
$env:VITE_API_BASE_URL = "http://127.0.0.1:5008/dapi"
$env:VITE_BACKEND_ORIGIN = "http://127.0.0.1:5008"
npm run build
xcopy /E /I /Y dist ..\desktop\frontend-dist
```

### 步骤 2：发布后端

```powershell
cd D:\作业\ai-image-workbench\backend
dotnet publish src/AI.Image.HttpApi.Host/AI.Image.HttpApi.Host.csproj -c Release -r win-x64 --self-contained true -o ../desktop/backend-dist
```

### 步骤 3：测试开发态 Electron

```powershell
cd D:\作业\ai-image-workbench\desktop
$env:NODE_OPTIONS = ""
npm start
```

### 步骤 4：构建目录版并测试

```powershell
npm run build:dir
```

测试程序：

```text
dist-electron/win-unpacked/AI 图片筛选与评审工作台.exe
```

### 步骤 5：构建正式安装包

```powershell
npm run build:nsis
```

## 十一、常见问题说明

### 1. 页面白屏

常见原因：

- frontend-dist 未更新。
- 前端未重新 build。
- Vite 未配置 base 为 ./。
- Vue Router 未使用 createWebHashHistory。
- 前端资源路径错误。
- 构建时没有设置 VITE_API_BASE_URL 和 VITE_BACKEND_ORIGIN。

### 2. 后端无法启动

常见原因：

- backend-dist 不完整。
- AI.Image.HttpApi.Host.exe 路径不正确。
- cwd 设置错误。
- 后端端口被占用。
- 发布目录缺少配置文件。

### 3. 导出接口请求成 file:///...

原因：

- 前端代码中直接使用了相对地址。
- 打包后页面运行于 file 协议，不再经过 Vite 代理。

解决方式：

- 使用完整 API 地址。
- 或统一使用带环境变量的请求基地址。

### 4. 安装包构建慢

首次执行 Electron 打包较慢属于正常现象，因为会下载：

- Electron 运行时。
- NSIS。
- electron-builder 依赖工具。

### 5. 构建时报符号链接权限错误

可尝试：

- 使用管理员 PowerShell。
- 开启 Windows 开发者模式。
- 删除 electron-builder 缓存后重试。

清理缓存命令：

```powershell
Remove-Item -Recurse -Force "$env:LOCALAPPDATA\electron-builder\Cache"
```

### 6. 图标相关错误

如果提示找不到 icon.ico，说明构建配置里仍引用了图标文件，但目录中没有该文件。处理方式：

1. 补充 icon.ico。
2. 或删除 desktop/package.json 中的 icon 配置后重新构建。

## 十二、维护建议

### 1. 后端建议增加 /health 接口

Electron 启动时使用健康检查接口判断后端是否就绪，比调用现有业务接口更稳定。

### 2. 上传目录建议不要写到安装目录

正式安装版中，程序安装目录可能没有写权限。

建议将上传目录、缓存目录、日志目录放到用户可写位置，例如：

```text
%LocalAppData%/AIImageWorkbench/
```

### 3. 发布前建议至少检查以下功能

- 项目列表加载。
- 项目详情加载。
- 图片详情打开。
- 对比评审功能。
- 上传功能。
- 导出功能。
- 关闭应用后后端退出。
- 安装与卸载流程。

## 十三、当前推荐命令汇总

### 前端构建

```powershell
cd D:\作业\ai-image-workbench\frontend
$env:VITE_API_BASE_URL = "http://127.0.0.1:5008/dapi"
$env:VITE_BACKEND_ORIGIN = "http://127.0.0.1:5008"
npm run build
xcopy /E /I /Y dist ..\desktop\frontend-dist
```

### 后端发布

```powershell
cd D:\作业\ai-image-workbench\backend
dotnet publish src/AI.Image.HttpApi.Host/AI.Image.HttpApi.Host.csproj -c Release -r win-x64 --self-contained true -o ../desktop/backend-dist
```

### Electron 开发态运行

```powershell
cd D:\作业\ai-image-workbench\desktop
$env:NODE_OPTIONS = ""
npm start
```

### 构建目录版

```powershell
npm run build:dir
```

### 构建安装包

```powershell
npm run build:nsis
```

## 十四、说明

本目录中的 Electron 仅作为桌面壳，不改变原有前后端业务实现逻辑。

- 前端仍由 Vue 负责页面展示。
- 后端仍由 ABP 与 .NET 提供业务 API。
- Electron 负责将其整合为 Windows 桌面应用。

后续若增加自动更新、日志收集、单实例运行等功能，可继续在本目录扩展。
