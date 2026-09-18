# Tasks

## 1. Implementation

- [x] 1.1 修正 authenticated 响应的旧服务端版本比较，并通过源码断言验证两个分支方向
- [x] 1.2 将 `Moveable.Track` 改为向所有非拥有者连接广播，并通过源码断言验证不再使用单一 `Find`

## 2. Verification

- [x] 2.1 运行 CodeGraph 同步、OpenSpec 严格验证和 `git diff --check`；构建验证因当前环境没有 .NET SDK 而跳过
