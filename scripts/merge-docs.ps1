$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$docs = Join-Path $root "docs"
$out = Join-Path $docs "药品管理系统-完整文档.md"

function Read-Lines([string]$path) { Get-Content -Path $path -Encoding UTF8 -Raw }

function Strip-ReqHeader([string]$text) {
    $text = $text -replace '(?s)^# 药品管理综合系统 — 需求分析文档\r?\n\r?\n\| 项目 \| 内容 \|\r?\n(?:\|[^\r\n]+\|\r?\n)+?\r?\n---\r?\n\r?\n', ''
    $text = $text -replace '(?s)### 1\.3 相关文档\r?\n\r?\n\| 文档 \| 说明 \|\r?\n(?:\|[^\r\n]+\|\r?\n)+?\r?\n', "### 1.3 本文档结构`n`n本篇为《药品管理综合系统 — 完整项目文档》**第二篇**；第三篇为系统设计，第四篇为业务流程，第五篇为数据库 ER，第六篇为部署操作，第七篇为答辩材料。`n`n"
    return $text.TrimStart()
}

function Strip-DesignHeader([string]$text) {
    $text = $text -replace '(?s)^# 药品管理综合系统 — 系统设计文档\r?\n\r?\n\| 项目 \| 内容 \|\r?\n(?:\|[^\r\n]+\|\r?\n)+?\r?\n---\r?\n\r?\n', ''
    $text = $text -replace '\[需求分析\.md\]\(需求分析\.md\)', '第二篇'
    return $text.TrimStart()
}

function Renumber-Part([string]$text, [string]$prefix) {
    return ($prefix + "`n`n" + $text)
}

function Strip-PartTitle([string]$text) {
    return ($text -replace '^# [^\r\n]+\r?\n\r?\n', '').TrimStart()
}

$part1 = @"
# 药品管理综合系统 — 完整项目文档

| 项目 | 内容 |
|------|------|
| 文档版本 | 1.0（合并版） |
| 编写日期 | 2026-06-28 |
| 说明 | 合并原 docs 目录 8 份 Markdown；Word 版见同目录 .docx |

> **图表说明**：文中 Mermaid 图在 Word 中显示为代码块；完整渲染请用 Markdown 预览或重新导出 PDF。

---

# 第一篇 项目概述

## 1.1 系统简介

基于 .NET 10 的药品进销存管理系统，采用分层架构：Core → Infrastructure → Services → WebAPI，前端为 Blazor Server。

## 1.2 功能模块

| 模块 | 说明 |
|------|------|
| **运营仪表盘** | 库存估值、低库存/临期统计、Top10 动销、智能补货建议 |
| **药品台账** | 录入、编辑、软下架、分页搜索、在售/已下架筛选、CSV 导出 |
| **库存预警** | 低库存监控，可跳转采购单、导出清单 |
| **效期预警** | 30/60/90 天临期批次，支撑 FEFO 策略 |
| **采购单** | 低库存一键生成 → 提交 → Admin 审核 → 收货入库 |
| **入库/出库** | 批次入库、FEFO 出库、流水分页筛选与 CSV 导出 |
| **供应商** | 供应商 CRUD，台账/预警/采购单关联展示 |
| **用户管理** | Admin 创建 Operator / Viewer 账号 |
| **审计日志** | Admin 查看 EF 变更审计（JSON diff） |

## 1.3 系统架构（概要）

```
Blazor Server (UI) ──JWT──▶ WebAPI ──▶ Services ──▶ Repositories ──▶ MySQL
                              │                      │
                              └── Redis (缓存)        └── 审计拦截器
```

## 1.4 创新点（三智一合规）

1. **智预警**：低库存 + 多档效期 + 智能补货三维预警
2. **智出库**：FEFO 批次先到期先出，降低过期损耗
3. **智分析**：运营仪表盘（库存估值、动销 Top10、补货建议）
4. **合规追溯**：库存流水 + EF 变更审计日志

## 1.5 核心业务流程（一句话）

低库存预警 → 一键生成采购单 → Admin 审核 → 收货入库（自动写批次+流水）→ FEFO 出库

## 1.6 角色权限（概要）

| 角色 | 权限概要 |
|------|----------|
| Admin | 全部 + 用户管理 + 审核采购单 + 下架药品 |
| Operator | 录入/编辑/入库出库/创建采购单 |
| Viewer | 只读 |

## 1.7 访问与默认账号

| 地址 | 用途 |
|------|------|
| http://localhost:5100 | Blazor 前端 |
| http://localhost:5246/health | API 健康检查 |

默认账号：`admin` / `Admin@123`；演示账号：`operator1` / `Operator@123`，`viewer1` / `Viewer@123`

---

"@

$req = Strip-ReqHeader (Read-Lines (Join-Path $docs "需求分析.md"))
$design = Strip-DesignHeader (Read-Lines (Join-Path $docs "系统设计.md"))

$flow = Strip-PartTitle (Read-Lines (Join-Path $docs "BUSINESS-FLOW.md"))
$er = Strip-PartTitle (Read-Lines (Join-Path $docs "ER-DIAGRAM.md"))
$manual = Read-Lines (Join-Path $docs "MANUAL-STEPS.md")
$manual = $manual -replace '# 手动操作详细步骤', '# 第六篇 部署与操作'
$manual = $manual -replace '(?s)## 阶段 D：答辩材料\r?\n\r?\n\| 文档 \| 用途 \|\r?\n(?:\|[^\r\n]+\|\r?\n)+?\r?\n\r?\n截图建议', "## 阶段 D：答辩材料`n`n答辩与 PPT 内容见**第七篇**。`n`n截图建议"

$defense = Strip-PartTitle (Read-Lines (Join-Path $docs "答辩素材.md"))
$defense = $defense -replace '详见 \[ARCHITECTURE\.md\].*?\。', '详见本文第一篇～第五篇。'
$ppt = Strip-PartTitle (Read-Lines (Join-Path $docs "PPT-OUTLINE.md"))
$ppt = $ppt -replace '引用 \[ARCHITECTURE\.md\].*', '引用本文第三篇系统架构'
$ppt = $ppt -replace '引用 \[ER-DIAGRAM\.md\].*', '引用本文第五篇数据库设计'
$ppt = $ppt -replace '引用 \[BUSINESS-FLOW\.md\].*', '引用本文第四篇业务流程'
$ppt = $ppt -replace '按 \[答辩素材\.md\].*插入：', '按第七篇 7.1 截图清单插入：'

$merged = $part1
$merged += "# 第二篇 需求分析`n`n" + $req + "`n`n---`n`n"
$merged += "# 第三篇 系统设计`n`n" + $design + "`n`n---`n`n"
$merged += "# 第四篇 业务流程`n`n" + $flow + "`n`n---`n`n"
$merged += "# 第五篇 数据库设计`n`n" + $er + "`n`n---`n`n"
$merged += $manual + "`n`n---`n`n"
$merged += "# 第七篇 答辩材料`n`n## 7.1 答辩素材`n`n" + $defense + "`n`n## 7.2 PPT 建议大纲`n`n" + $ppt

[System.IO.File]::WriteAllText($out, $merged, [System.Text.UTF8Encoding]::new($false))
Write-Host "Merged: $out ($((Get-Content $out | Measure-Object -Line).Lines) lines)"
