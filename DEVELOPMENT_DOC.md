# 杂货铺系统开发文档

## 1. 项目背景

当前工作区是一个 GemDesign 导出的静态页面演示，入口文件为 `index.html`，移动端演示页面位于 `assets/page/`：

- `password-verify-page.html`：访问密码验证页。
- `category-card-list-page.html`：分类 + 卡片列表页。
- `article_detail.html`：卡片内容详情页。

现有页面整体偏移动端 H5 风格，已经表达出核心业务链路：用户输入访问密码，进入分类卡片列表，点击卡片查看图文详情。后续系统需要把静态内容改造成可后台维护、可接口驱动、可部署运行的完整系统。

## 2. 建设目标

开发一个“杂货铺内容展示系统”，包含移动端展示端、后台管理端和 C# API 服务。

移动端用于访客浏览分类和卡片内容；后台管理端用于维护访问密码、基础配置、分类、卡片、标签、标题、正文图文内容和排序状态；C# API 负责认证、数据读写、文件上传和移动端内容输出。

## 3. 现有演示风格提炼

### 3.1 移动端整体

- 设计尺寸以手机为主，演示默认宽度约 `390px - 440px`。
- 页面使用固定顶部栏、左侧分类栏、卡片网格、详情阅读页。
- 圆角、阴影、隐藏滚动条、紧凑卡片布局是主要视觉特征。

### 3.2 密码验证页

- 背景：浅粉色系，叠加低透明度背景图。
- 品牌：页面顶部显示店铺名称和两行说明文案。
- 输入：6 位数字密码格，支持明文/密文切换。
- 键盘：页面内数字键盘，圆形按键。
- 反馈：密码错误时提示并抖动。

### 3.3 分类卡片列表页

- 背景：深色主题。
- 顶部：固定标题栏，显示店铺名称。
- 左侧：固定分类菜单，当前分类高亮。
- 主区：两列卡片网格。
- 卡片：图片、标题、副标题/简介，深色卡片背景和阴影，按压时缩放反馈。

### 3.4 文章详情页

- 背景：深色阅读页。
- 顶部：固定返回栏和标题。
- 内容：标题、标签、分割线、正文段落、图片和图片说明。
- 排版：正文 15px 左右，行高约 1.8，标签为浅蓝底深蓝字。

## 4. 技术方案

### 4.1 后端

- 语言/框架：C# + ASP.NET Core Web API。
- ORM：Entity Framework Core。
- 数据库：SQL Server。
- 认证：后台使用 JWT 或 Cookie 登录态；移动端访问密码单独校验。
- 文件：服务器本地磁盘存储，建议目录为 `wwwroot/uploads`。
- 文档：Swagger/OpenAPI。

### 4.2 后台管理端

- UI 框架：Bootstrap 5。
- 形态：可选 ASP.NET Core MVC/Razor Pages，或独立前端调用 API。
- 第一阶段建议使用 Razor Pages + Bootstrap，开发快、部署简单。

### 4.3 移动端展示端

- 形态：H5 页面。
- 样式：复用当前演示风格，可保留 Tailwind 思路，也可转为常规 CSS。
- 数据：通过 API 获取系统配置、分类、卡片列表和详情内容。

## 5. 功能范围

### 5.1 移动端

1. 访问密码验证
   - 输入数字密码。
   - 支持显示/隐藏密码。
   - 密码错误提示。
   - 密码通过后进入分类卡片列表。

2. 分类卡片列表
   - 展示店铺名称。
   - 展示全部分类。
   - 支持按分类筛选卡片。
   - 支持卡片点击进入详情。
   - 卡片展示封面图、标题、简介、标签或状态。

3. 卡片详情
   - 展示标题。
   - 展示标签。
   - 展示图文内容。
   - 支持多图片、多段文字、图片说明。
   - 支持返回列表。

### 5.2 后台管理端

1. 登录和管理员密码
   - 管理员登录。
   - 修改后台登录密码。
   - 不需要多管理员和角色权限，第一期只保留单管理员账号。

2. 系统配置
   - 店铺名称。
   - 首页说明文案。
   - 移动端访问密码配置。
   - 移动端访问密码为全站统一密码。
   - 密码位数和启用状态。
   - 主题色配置可作为二期扩展。

3. 分类管理
   - 新增分类。
   - 编辑分类名称、图标、排序、启用状态。
   - 删除分类。
   - 支持拖拽排序或上下移动排序。

4. 卡片管理
   - 新增卡片。
   - 选择所属分类。
   - 上传封面图。
   - 编辑标题、副标题/简介。
   - 编辑标签。
   - 编辑状态：草稿、已发布、隐藏。
   - 编辑排序。
   - 删除卡片。

5. 内容管理
   - 卡片详情标题。
   - 标签维护。
   - 使用富文本编辑器维护详情内容。
   - 支持正文图片上传。
   - 支持图片说明。
   - 保存草稿、发布。

6. 上传管理
   - 上传封面图。
   - 上传正文图片。
   - 限制格式：jpg、jpeg、png、webp。
   - 限制大小：建议单图 5MB 内。
   - 返回可访问 URL。

7. 分享二维码
   - 后台输入或生成移动端分享链接。
   - 将分享链接转换为二维码图片。
   - 支持二维码预览和下载。
   - 二维码内容只承载访问链接，不额外增加分享统计。

## 6. 数据库设计

### 6.1 AdminUser 管理员表

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| Id | bigint | 主键 |
| Username | nvarchar(50) | 登录账号 |
| PasswordHash | nvarchar(255) | 密码哈希 |
| PasswordSalt | nvarchar(255) | 密码盐 |
| Status | int | 状态 |
| LastLoginAt | datetime | 最后登录时间 |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

### 6.2 SiteConfig 系统配置表

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| Id | bigint | 主键 |
| SiteName | nvarchar(100) | 店铺名称 |
| SiteSubtitle | nvarchar(200) | 副标题 |
| SiteDescription | nvarchar(500) | 描述文案 |
| AccessPasswordHash | nvarchar(255) | 全站移动端访问密码哈希 |
| AccessPasswordEnabled | bit | 是否启用访问密码 |
| Theme | nvarchar(max) | 主题 JSON，可选 |
| UpdatedAt | datetime | 更新时间 |

### 6.3 Category 分类表

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| Id | bigint | 主键 |
| Name | nvarchar(100) | 分类名称 |
| Slug | nvarchar(100) | 分类标识 |
| Icon | nvarchar(255) | 图标，可选 |
| SortOrder | int | 排序 |
| IsEnabled | bit | 是否启用 |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

### 6.4 Card 卡片表

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| Id | bigint | 主键 |
| CategoryId | bigint | 分类 ID |
| Title | nvarchar(200) | 标题 |
| Summary | nvarchar(500) | 简介 |
| CoverImageUrl | nvarchar(500) | 封面图 |
| Status | int | 草稿/发布/隐藏 |
| SortOrder | int | 排序 |
| ViewCount | int | 浏览量 |
| PublishedAt | datetime | 发布时间 |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

### 6.5 Tag 标签表

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| Id | bigint | 主键 |
| Name | nvarchar(50) | 标签名 |
| Color | nvarchar(30) | 标签颜色，可选 |
| CreatedAt | datetime | 创建时间 |

### 6.6 CardTag 卡片标签关系表

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| CardId | bigint | 卡片 ID |
| TagId | bigint | 标签 ID |

### 6.7 CardContent 卡片内容表

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| Id | bigint | 主键 |
| CardId | bigint | 卡片 ID |
| ContentType | int | 1 文本，2 图片，3 富文本 |
| TextContent | nvarchar(max) | 文本内容 |
| ImageUrl | nvarchar(500) | 图片地址 |
| ImageCaption | nvarchar(200) | 图片说明 |
| SortOrder | int | 排序 |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

第一期详情正文使用富文本编辑器维护，建议在 `Card` 或独立详情表中保存 `ContentHtml`。如后续需要结构化图文块排序，再扩展为 `CardContent` 分块表。

### 6.8 UploadFile 上传文件表

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| Id | bigint | 主键 |
| OriginalName | nvarchar(255) | 原文件名 |
| FileName | nvarchar(255) | 存储文件名 |
| FileUrl | nvarchar(500) | 访问地址 |
| FileSize | bigint | 文件大小 |
| MimeType | nvarchar(100) | 文件类型 |
| UsageType | nvarchar(50) | cover/content/config |
| CreatedAt | datetime | 上传时间 |

## 7. API 接口设计

### 7.1 认证接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| POST | `/api/admin/auth/login` | 后台登录 |
| POST | `/api/admin/auth/logout` | 后台退出 |
| POST | `/api/admin/auth/change-password` | 修改管理员密码 |
| POST | `/api/mobile/access/verify` | 校验移动端访问密码 |

### 7.2 系统配置接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| GET | `/api/admin/config` | 获取后台配置 |
| PUT | `/api/admin/config` | 保存系统配置 |
| GET | `/api/mobile/config` | 获取移动端公开配置 |

### 7.3 分类接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| GET | `/api/admin/categories` | 后台分类列表 |
| POST | `/api/admin/categories` | 新增分类 |
| PUT | `/api/admin/categories/{id}` | 修改分类 |
| DELETE | `/api/admin/categories/{id}` | 删除分类 |
| PUT | `/api/admin/categories/sort` | 分类排序 |
| GET | `/api/mobile/categories` | 移动端分类列表 |

### 7.4 卡片接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| GET | `/api/admin/cards` | 后台卡片列表 |
| POST | `/api/admin/cards` | 新增卡片 |
| GET | `/api/admin/cards/{id}` | 后台卡片详情 |
| PUT | `/api/admin/cards/{id}` | 修改卡片 |
| DELETE | `/api/admin/cards/{id}` | 删除卡片 |
| PUT | `/api/admin/cards/sort` | 卡片排序 |
| PUT | `/api/admin/cards/{id}/publish` | 发布/隐藏 |
| GET | `/api/mobile/cards` | 移动端卡片列表 |
| GET | `/api/mobile/cards/{id}` | 移动端卡片详情 |

### 7.5 标签接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| GET | `/api/admin/tags` | 标签列表 |
| POST | `/api/admin/tags` | 新增标签 |
| PUT | `/api/admin/tags/{id}` | 修改标签 |
| DELETE | `/api/admin/tags/{id}` | 删除标签 |

### 7.6 上传接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| POST | `/api/admin/uploads/image` | 上传图片 |
| DELETE | `/api/admin/uploads/{id}` | 删除上传文件 |

### 7.7 分享二维码接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| POST | `/api/admin/qrcode` | 将分享链接转换为二维码 |
| GET | `/api/admin/qrcode/download?url=xxx` | 下载分享链接二维码 |

## 8. 后台页面规划

后台使用 Bootstrap 5，建议页面如下：

1. 登录页
   - 用户名、密码、登录按钮。

2. 控制台
   - 分类数量、卡片数量、已发布数量、最近更新时间。

3. 系统配置页
   - 店铺名称。
   - 首页副标题和说明。
   - 移动端访问密码。
   - 是否启用密码验证。
   - 保存按钮。

4. 分类管理页
   - 表格：分类名称、排序、状态、创建时间、操作。
   - 操作：新增、编辑、删除、启用/禁用。

5. 卡片管理页
   - 筛选：分类、状态、关键词。
   - 表格：封面、标题、分类、标签、状态、排序、更新时间。
   - 操作：新增、编辑、预览、发布/隐藏、删除。

6. 卡片编辑页
   - 基础信息：分类、标题、简介、封面图、标签。
   - 内容编辑：富文本编辑器。
   - 操作：保存草稿、发布、预览。

7. 修改密码页
   - 原密码、新密码、确认密码。

8. 分享二维码页
   - 输入移动端页面链接。
   - 生成二维码。
   - 预览二维码。
   - 下载二维码图片。

## 9. 移动端页面规划

### 9.1 密码页

- 首次访问判断是否启用访问密码。
- 已验证通过可写入本地 session/token。
- 密码输入完成后调用 `/api/mobile/access/verify`。
- 验证成功跳转分类页。

### 9.2 分类卡片页

- 初始化调用 `/api/mobile/config` 和 `/api/mobile/categories`。
- 默认展示“全部”分类。
- 点击分类后调用 `/api/mobile/cards?categoryId=xxx` 或前端本地筛选。
- 点击卡片进入 `/detail.html?id=xxx`。

### 9.3 详情页

- 调用 `/api/mobile/cards/{id}`。
- 渲染标题、标签、正文图文内容。
- 保留当前深色阅读风格。

## 10. 权限与安全

- 后台接口必须登录后访问。
- 密码必须哈希存储，不能明文保存。
- 上传文件校验扩展名、MIME 类型和大小。
- 上传文件名使用随机名，避免覆盖和路径穿越。
- 删除分类前需要判断是否存在卡片。
- 删除卡片时保留上传文件，或进入待清理状态，避免误删复用图片。
- 移动端只返回已启用分类和已发布卡片。
- 不开发访问统计、搜索、分享统计、缓存和 SEO 功能。

## 11. 开发阶段

### 第一阶段：基础后端和数据库

- 创建 ASP.NET Core Web API 项目。
- 建立 EF Core 实体和迁移。
- 完成管理员登录、修改密码、系统配置。
- 完成 Swagger。

### 第二阶段：后台管理端

- Bootstrap 登录页。
- 系统配置页。
- 分类管理。
- 图片上传。
- 卡片管理和编辑。

### 第三阶段：移动端接口化

- 将静态密码页改成接口校验。
- 将分类卡片页改成接口读取。
- 将详情页改成接口读取图文内容。
- 保留演示页面的深色/粉色视觉风格。

### 第四阶段：联调和验收

- 后台新增分类和卡片后，移动端即时可见。
- 上传图片可在移动端正常展示。
- 密码修改后旧密码失效。
- 后台可把移动端分享链接转换成二维码。
- 手机尺寸下页面无横向溢出。
- 所有后台操作有成功/失败提示。

## 12. 验收标准

- 管理员可以登录后台并修改后台密码。
- 管理员可以配置移动端访问密码。
- 管理员可以新增、编辑、删除、排序分类。
- 管理员可以新增、编辑、删除、发布、隐藏卡片。
- 管理员可以上传封面图和正文图片。
- 卡片详情可以维护多个标签、标题、文字和图片。
- 管理员可以输入分享链接并生成二维码。
- 移动端输入正确密码后可进入列表。
- 移动端可按分类查看卡片。
- 移动端可查看完整图文详情。
- 移动端样式与当前演示保持一致：密码页粉色轻量、列表页深色卡片、详情页深色阅读。

## 13. 已确认范围

- 数据库使用 SQL Server。
- 后台不需要多管理员和角色权限。
- 移动端访问密码使用全站统一密码。
- 卡片详情使用富文本编辑器。
- 上传图片存储在服务器本地。
- 不需要访问统计、搜索、分享统计、缓存和 SEO。
- 后台需要提供分享链接转换二维码功能。
