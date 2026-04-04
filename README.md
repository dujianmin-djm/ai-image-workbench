# AI 图片筛选与评审工作台

基于 ABP Framework + Vue 3 的图片批量筛选与评审管理系统。用于对 AI 生成/设计稿/电商等场景的图片进行统一导入、评分、标注、对比和导出。

## 技术栈

| 层          | 技术                  | 版本   |
| ----------- | --------------------- | ------ |
| 后端框架    | ABP Framework         | 10.2.0 |
| 运行时      | .NET                  | 10     |
| ORM         | Entity Framework Core | —      |
| 数据库      | SQLite                | —      |
| 图片处理    | SixLabors.ImageSharp  | 3.1.12 |
| 对象映射    | Riok.Mapperly         | —      |
| 前端框架    | Vue                   | 3.5.30 |
| 构建工具    | Vite                  | 8.0.1  |
| UI 组件库   | Element Plus          | 2.13.6 |
| 状态管理    | Pinia                 | 3.0.4  |
| 路由        | Vue Router            | 5.0.4  |
| HTTP 客户端 | Axios                 | 1.14.0 |

## 项目结构

```
ai-image-workbench/
├── start.bat                   # 一键启动脚本
├── stop.bat                    # 停止服务脚本
├── backend/                    # 后端 (ABP Framework)
│   ├── src/
│   │   ├── AI.Image.Domain/              # 领域层：实体、枚举、仓储接口
│   │   │   ├── ImageSets/ImageItem.cs    # 图片条目聚合根
│   │   │   └── Books/WorkProject.cs      # 工作项目聚合根
│   │   ├── AI.Image.Domain.Shared/       # 共享层：枚举、常量
│   │   │   └── ImageSets/ReviewStatus.cs # 评审状态枚举
│   │   ├── AI.Image.Application.Contracts/ # 应用契约层：DTO、服务接口
│   │   │   ├── ImageSets/               # 图片相关 DTO
│   │   │   └── Services/                # 项目相关 DTO
│   │   ├── AI.Image.Application/         # 应用层：服务实现
│   │   │   ├── ImageSets/ImageItemAppService.cs   # 图片服务
│   │   │   └── WorkProjects/WorkProjectAppService.cs # 项目服务
│   │   ├── AI.Image.EntityFrameworkCore/ # EF Core：DbContext、迁移
│   │   ├── AI.Image.HttpApi/            # HTTP API 层
│   │   └── AI.Image.HttpApi.Host/       # 宿主程序
│   │       ├── appsettings.json         # 配置（数据库连接串等）
│   │       └── uploads/                 # 上传文件存储目录
│   └── common.props
├── frontend/                   # 前端 (Vue 3 + TypeScript)
│   ├── src/
│   │   ├── api/
│   │   │   ├── request.ts              # Axios 实例与响应拦截
│   │   │   ├── imageApi.ts             # 图片 API 封装
│   │   │   └── projectApi.ts           # 项目 API 封装
│   │   ├── stores/
│   │   │   ├── imageStore.ts           # 图片 Pinia Store
│   │   │   └── projectStore.ts         # 项目 Pinia Store
│   │   ├── router/index.ts             # 路由定义
│   │   ├── views/
│   │   │   ├── ProjectList.vue         # 项目列表
│   │   │   ├── ProjectDetail.vue       # 项目详情/图片网格
│   │   │   ├── ImageDetail.vue         # 图片详情/评审
│   │   │   └── Compare.vue            # 对比评审
│   │   └── layouts/                    # 布局组件
│   ├── vite.config.ts
│   └── package.json
```

## 环境要求

- **.NET 10 SDK**（[下载](https://dotnet.microsoft.com/download)）
- **Node.js 18+**（[下载](https://nodejs.org/)）
- npm（随 Node.js 自带）

## 快速开始

### 方式一：一键启动

双击项目根目录下的 `start.bat`，脚本将自动：

1. 检测 .NET SDK 和 Node.js 环境
2. 安装前端依赖（首次运行）
3. 还原后端 NuGet 包
4. 执行数据库迁移
5. 启动后端服务（`http://localhost:5008`）
6. 启动前端开发服务（`http://localhost:5007`）
7. 打开浏览器

停止服务：运行 `stop.bat`。

### 方式二：手动启动

**后端：**

```bash
cd backend
dotnet restore src/AI.Image.HttpApi.Host/AI.Image.HttpApi.Host.csproj
dotnet ef database update \
  --project src/AI.Image.EntityFrameworkCore/AI.Image.EntityFrameworkCore.csproj \
  --startup-project src/AI.Image.HttpApi.Host/AI.Image.HttpApi.Host.csproj
dotnet run --project src/AI.Image.HttpApi.Host --urls http://localhost:5008
```

**前端：**

```bash
cd frontend
npm install
npm run dev
```

### 访问地址

| 服务         | URL                           |
| ------------ | ----------------------------- |
| 前端         | http://localhost:5007         |
| 后端 API     | http://localhost:5008         |
| Swagger 文档 | http://localhost:5008/swagger |

## 数据库

使用 SQLite，数据库文件 `Image.db` 生成在后端 Host 项目运行目录下。

连接字符串（`backend/src/AI.Image.HttpApi.Host/appsettings.json`）：

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=Image.db;"
  }
}
```

### 执行迁移

```bash
cd backend
# 添加新迁移
dotnet ef migrations add <MigrationName> \
  --project src/AI.Image.EntityFrameworkCore \
  --startup-project src/AI.Image.HttpApi.Host

# 更新数据库
dotnet ef database update \
  --project src/AI.Image.EntityFrameworkCore \
  --startup-project src/AI.Image.HttpApi.Host
```

## 域模型

### ImageItem（图片条目）

| 字段           | 类型         | 说明                                    |
| -------------- | ------------ | --------------------------------------- |
| Id             | Guid         | 主键                                    |
| ProjectId      | Guid         | 所属项目                                |
| FileName       | string       | 原始文件名                              |
| FilePath       | string       | 相对存储路径（`uploads/...`）           |
| ThumbnailPath  | string?      | 缩略图路径（自动生成，320px 宽）        |
| FileSize       | long         | 文件大小（字节）                        |
| Width / Height | int          | 图片尺寸（像素）                        |
| MimeType       | string?      | MIME 类型                               |
| Rating         | int          | 星级评分（0-5，0=未评）                 |
| Status         | ReviewStatus | 评审状态：Pending / Selected / Rejected |
| Notes          | string?      | 评审备注                                |
| TagsJson       | string?      | 标签（JSON 数组字符串）                 |
| SortOrder      | int          | 排序序号                                |

### WorkProject（工作项目）

| 字段        | 类型    | 说明                                                          |
| ----------- | ------- | ------------------------------------------------------------- |
| Id          | Guid    | 主键                                                          |
| Name        | string  | 项目名称                                                      |
| Description | string? | 描述                                                          |
| Template    | string? | 模板类型（ai-gen / design / ecommerce / other / heart-valve） |
| CoverPath   | string? | 封面图片路径                                                  |
| ImageCount  | int     | 图片数量（缓存值，随增删自动更新）                            |

## API 参考

所有 API 以 `/dapi` 为前缀，响应统一包装为：

```json
{
  "code": "200",
  "success": true,
  "data": { ... },
  "message": "success"
}
```

> 文件流类型的响应（如导出 ZIP）不经过包装，直接返回二进制流。

### 项目接口 `/dapi/project`

| 方法   | 路径           | 说明                         | 参数                                      |
| ------ | -------------- | ---------------------------- | ----------------------------------------- |
| GET    | `/{id}`        | 获取项目详情                 | `id`: Guid                                |
| GET    | `/query`       | 分页查询项目                 | `Current`, `Size`, `Sorts`                |
| POST   | `/add`         | 创建项目                     | Body: `{ name, description?, template? }` |
| PUT    | `/edit/{id}`   | 更新项目                     | Body: `{ name, description?, template? }` |
| DELETE | `/delete/{id}` | 删除项目（级联删除所有图片） | `id`: Guid                                |

### 图片接口 `/dapi/image`

| 方法   | 路径                           | 说明                            | 参数                                                                  |
| ------ | ------------------------------ | ------------------------------- | --------------------------------------------------------------------- |
| GET    | `/{id}`                        | 获取图片详情                    | `id`: Guid                                                            |
| GET    | `/query`                       | 分页查询图片                    | `ProjectId`, `Status`, `MinRating`, `Tag`, `Current`, `Size`, `Sorts` |
| POST   | `/upload?projectId={id}`       | 上传图片（multipart/form-data） | `files`: 文件列表                                                     |
| PUT    | `/edit/{id}`                   | 更新图片信息                    | Body: `{ rating?, status?, notes?, tagsJson? }`                       |
| DELETE | `/delete/{id}`                 | 删除图片                        | `id`: Guid                                                            |
| DELETE | `/deleteByProject/{projectId}` | 删除项目下所有图片              | `projectId`: Guid                                                     |
| POST   | `/export`                      | 导出图片为 ZIP                  | Body: `{ ids?: Guid[], projectId?: Guid }`                            |

**查询参数说明：**

- `MinRating`: `-1` 表示筛选未评分图片（Rating == 0），`1-5` 表示最低星级
- `Status`: `Pending` / `Selected` / `Rejected`
- `Sorts`: 排序字段，如 `creationTime desc`

**支持的图片格式：** `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`, `.bmp`, `.tiff`

上传时自动生成 320px 宽度的缩略图，存储在 `uploads/thumbnails/` 目录。

## 前端路由

| 路径                    | 视图              | 说明                                 |
| ----------------------- | ----------------- | ------------------------------------ |
| `/`                     | —                 | 重定向到 `/projects`                 |
| `/projects`             | ProjectList.vue   | 项目列表                             |
| `/projects/:id`         | ProjectDetail.vue | 项目详情、图片网格、筛选、上传、导出 |
| `/projects/:id/compare` | Compare.vue       | 对比评审（双栏）                     |
| `/image/:id`            | ImageDetail.vue   | 图片详情、快速评审                   |

## 前端 API 请求约定

- Axios 实例基础路径：`/dapi`，超时 30 秒
- Vite 开发代理：`/dapi` → `http://localhost:5008`
- 响应拦截器自动解包 `ApiResponse`，业务代码仅处理 `data` 部分
- 图片文件 URL 直接拼接后端地址：`http://localhost:5008/{filePath}`

## 构建

**前端生产构建：**

```bash
cd frontend
npm run build     # 包含 TypeScript 类型检查
```

产物输出到 `frontend/dist/`。

**后端发布：**

```bash
cd backend
dotnet publish src/AI.Image.HttpApi.Host/AI.Image.HttpApi.Host.csproj -c Release -o publish
```

## 开发说明

- 后端遵循 ABP 分层架构，服务类继承自定义 `AppService` 基类
- API 路由使用 `[Route("dapi/xxx")]` + HTTP Method 特性，不走 ABP 自动 API 控制器
- 响应包装通过 `ApiResultFilter`（ActionFilter）和 `ApiResponseHandlerMiddleware` 实现
- 对象映射使用 Mapperly（编译时源生成器），映射类位于各 Application 子目录
- 文件上传存储在 `backend/src/AI.Image.HttpApi.Host/uploads/` 目录下
- SQLite 数据库禁用了 EF Core 工作单元事务（兼容性优化）
