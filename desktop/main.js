const { app, BrowserWindow, dialog } = require("electron");
const { spawn } = require("child_process");
const path = require("path");

let mainWindow = null;
let backendProcess = null;

const isPackaged = app.isPackaged;

const frontendDir = isPackaged
  ? path.join(process.resourcesPath, "frontend-dist")
  : path.join(__dirname, "frontend-dist");

const backendDir = isPackaged
  ? path.join(process.resourcesPath, "backend-dist")
  : path.join(__dirname, "backend-dist");

const frontendIndex = path.join(frontendDir, "index.html");
const backendExe = path.join(backendDir, "AI.Image.HttpApi.Host.exe");

const BACKEND_URL = "http://127.0.0.1:5008";
const HEALTH_URL = `${BACKEND_URL}/health-status`;

function startBackend() {
  return new Promise((resolve, reject) => {
    console.log("isPackaged:", isPackaged);
    console.log("frontendIndex:", frontendIndex);
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

async function waitForBackend(retries = 30, interval = 1000) {
  for (let i = 0; i < retries; i++) {
    try {
      const res = await fetch(HEALTH_URL);
      console.log("Backend health statusCode:", res.status);

      if (res.ok) {
        const data = await res.json();
        console.log("Backend health response:", data);

        if (data?.status === "Healthy") {
          return;
        }
      }
    } catch (err) {
      console.log("Backend health check failed:", err.message);
    }

    await new Promise((resolve) => setTimeout(resolve, interval));
  }

  throw new Error("后端启动失败：健康检查超时");
}

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

  // 调试时打开
  // mainWindow.webContents.openDevTools();

  mainWindow.webContents.on("did-fail-load", (_, code, desc, url) => {
    console.error("did-fail-load:", code, desc, url);
    dialog.showErrorBox(
      "前端页面加载失败",
      `code: ${code}\ndesc: ${desc}\nurl: ${url}`
    );
  });

  mainWindow.on("closed", () => {
    mainWindow = null;
  });
}

function stopBackend() {
  if (backendProcess) {
    try {
      backendProcess.kill();
    } catch (err) {
      console.error("停止后端失败:", err);
    }
    backendProcess = null;
  }
}

app.whenReady().then(async () => {
  try {
    await startBackend();
    await waitForBackend();
    createWindow();
  } catch (err) {
    console.error("应用启动失败:", err);
    dialog.showErrorBox("应用启动失败", err.message || String(err));
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