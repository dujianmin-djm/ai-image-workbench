# AI 图片筛选与评审工作台

基于 ABP Framework、Vue 3 与 Electron 的图片批量筛选与评审系统，用于支持 AI 生图、设计稿、电商素材等场景中的导入、浏览、评审、对比与导出。

当前项目同时支持两种运行模式：

- Web 开发调试模式：前后端分离运行，适合开发与联调。
- Windows 桌面版：通过 Electron 封装前端静态资源与本地 .NET 后端，适合演示与单机交付。

## 文档导航

- [DESIGN.md](DESIGN.md)：产品设计文档，包含目标用户、场景分析、交互设计与关键决策记录。
- [AI_USAGE.md](AI_USAGE.md)：AI 使用说明，说明本项目中 AI 的参与环节与采纳原则。
- [USER-MANUAL.md](USER-MANUAL.md)：面向最终用户的操作手册。
- [desktop/README.md](desktop/README.md)：Electron 桌面打包与发布说明。

## 技术栈

| 层级 | 技术 | 版本 |
| --- | --- | --- |
| 后端框架 | ABP Framework | 10.2.0 |
| 运行时 | .NET | 10 |
| ORM | Entity Framework Core | 10 |
| 数据库 | SQLite | 本地单机 |
| 图片处理 | SixLabors.ImageSharp | 3.1.12 |
| 对象映射 | Riok.Mapperly | 编译期生成 |
| 前端框架 | Vue | 3.5.30 |
| 构建工具 | Vite | 8.0.1 |
| UI 组件库 | Element Plus | 2.13.6 |
| 状态管理 | Pinia | 3.0.4 |
| 路由 | Vue Router | 5.0.4 |
| 桌面容器 | Electron | 41.1.1 |
| 打包工具 | electron-builder | 26.8.1 |

## 功能概览

- 项目维度管理图片批次。
- 批量导入图片并自动生成缩略图。
- 按状态、评分进行筛选。
- 直接在卡片层进行快捷评分和状态切换。
- 进入图片详情页做完整评审。
- 选中两张图片进入对比评审。
- 勾选当前结果并导出 ZIP。
- 打包为 Windows 桌面应用。

## 目录结构

```text
ai-image-workbench/
├── backend/                  # ABP 后端
├── frontend/                 # Vue 前端
├── desktop/                  # Electron 桌面封装
├── README.md                 # 项目总览与开发说明
├── DESIGN.md                 # 产品设计文档
├── AI_USAGE.md               # AI 使用说明
├── USER-MANUAL.md            # 用户操作手册
```

## 运行模式

| 模式 | 适用场景 | 启动方式 |
| --- | --- | --- |
| Web 开发调试 | 代码开发、接口联调、问题排查 | 分别启动 frontend 与 backend |
| Electron 桌面版 | 演示、目录版测试、安装包交付 | 参考 [desktop/README.md](desktop/README.md) 构建与启动 |

## 示例图片

<img width="3072" height="1822" alt="project2" src="https://github.com/user-attachments/assets/0aa7563e-3dbe-4695-9a37-2a1ea9673a6a" />
<img width="3072" height="1824" alt="project3" src="https://github.com/user-attachments/assets/726392d7-fa3c-4ec6-8261-162a1eb97c10" />
<img width="3072" height="1824" alt="project4" src="https://github.com/user-attachments/assets/11753a1a-11c0-4843-8762-714adf05a4d4" />
<img width="3072" height="1822" alt="image-compare" src="https://github.com/user-attachments/assets/4d76edf4-b949-4b5a-ad43-4d38b6977732" />
<img width="3072" height="1824" alt="image-detail" src="https://github.com/user-attachments/assets/58760366-81bd-4f42-ade4-e27420c40954" />
<img width="1920" height="1080" alt="image-openapi" src="https://github.com/user-attachments/assets/26f144d3-32ff-4911-8936-b60c284017b0" />

## 快速开始

### 1. 开发环境要求

- .NET 10 SDK。
- Node.js 18 或更高版本。
- npm。

### 2. Web 开发调试

```powershell
cd D:\作业\ai-image-workbench\backend
dotnet restore src\AI.Image.HttpApi.Host\AI.Image.HttpApi.Host.csproj
dotnet ef database update --project src\AI.Image.EntityFrameworkCore\AI.Image.EntityFrameworkCore.csproj --startup-project src\AI.Image.HttpApi.Host\AI.Image.HttpApi.Host.csproj
dotnet run --project src\AI.Image.HttpApi.Host --urls http://localhost:5008
```

```powershell
cd D:\作业\ai-image-workbench\frontend
npm install
npm run dev
```

开发态访问地址：

- 前端：http://localhost:5007
- 后端：http://localhost:5008
- Swagger：http://localhost:5008/swagger

### 3. 桌面版打包与测试

桌面版构建流程已整理到 [desktop/README.md](desktop/README.md)，核心步骤如下：

1. 构建前端静态资源，并复制到 desktop/frontend-dist。
2. 发布 .NET 后端到 desktop/backend-dist。
3. 在 desktop 目录执行 npm start 验证开发态 Electron。
4. 执行 npm run build:dir 生成目录版并测试。
5. 执行 npm run build:nsis 生成安装包。

## 桌面端适配说明

由于 Electron 最终加载的是 file 协议下的前端静态资源，当前项目已做以下适配：

- Vite 的 base 设置为 ./。
- Vue Router 使用 createWebHashHistory。
- 前端接口访问使用显式环境变量 VITE_API_BASE_URL。
- 图片静态资源访问使用显式环境变量 VITE_BACKEND_ORIGIN。
- Electron 主进程在应用启动时自动拉起本地 .NET 后端。

桌面版构建前，建议在前端构建命令前设置：

```powershell
$env:VITE_API_BASE_URL = "http://127.0.0.1:5008/dapi"
$env:VITE_BACKEND_ORIGIN = "http://127.0.0.1:5008"
```

## 核心业务对象

### WorkProject

表示一个图片评审批次，核心字段包括：

- Name：项目名称。
- Description：项目描述。
- Template：模板或业务类型。
- CoverPath：封面图路径。
- ImageCount：项目下图片数量缓存。

### ImageItem

表示一张被纳入评审流程的图片，核心字段包括：

- FileName、FilePath、ThumbnailPath。
- Width、Height、FileSize、MimeType。
- Rating，范围为 0 到 5。
- Status，支持待评、选中、淘汰。
- Notes、TagsJson。

## 核心 API 概览

所有接口统一以 /dapi 为前缀。

### 项目接口

- GET /dapi/project/query：分页查询项目。
- GET /dapi/project/{id}：获取项目详情。
- POST /dapi/project/add：创建项目。
- PUT /dapi/project/edit/{id}：编辑项目。
- DELETE /dapi/project/delete/{id}：删除项目及其图片。

### 图片接口

- GET /dapi/image/query：分页查询图片。
- GET /dapi/image/{id}：获取图片详情。
- POST /dapi/image/upload：批量上传图片。
- PUT /dapi/image/edit/{id}：修改图片评审信息。
- DELETE /dapi/image/delete/{id}：删除图片。
- DELETE /dapi/image/deleteByProject/{projectId}：删除项目下全部图片。
- POST /dapi/image/export：导出 ZIP。

补充说明：

- MinRating 为 -1 时表示筛选未评分图片。
- 导出接口返回文件流，不走统一 JSON 包装。
- 上传时会自动生成缩略图。

## 数据与文件存储

当前版本使用 SQLite 作为本地数据库，连接字符串为：

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=Image.db;"
  }
}
```

开发态下，数据库与上传目录位于后端运行目录。桌面安装版当前仍使用 backend-dist 作为运行工作目录，后续建议迁移到用户可写目录，例如 LocalAppData。

## 开发说明

- 后端遵循 ABP 分层架构。
- API 路由使用显式 Route 特性，不依赖 ABP 自动控制器。
- 对象映射使用 Mapperly。
- 图片上传使用 ImageSharp 生成缩略图。
- 前端使用 Pinia 管理项目与图片状态。
- 项目详情页统一使用选择集实现导出与对比入口。

## 后续可优化点

- 增加专用健康检查接口替代当前业务接口轮询。
- 将桌面版数据库、上传目录迁移到用户可写目录。
- 完善大列表性能优化与键盘操作。
- 增加自动更新、日志和异常采集。
