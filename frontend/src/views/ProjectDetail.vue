<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useProjectStore } from '@/stores/projectStore'
import { useImageStore } from '@/stores/imageStore'
import { imageApi, type ImageItemDto, type ReviewStatus } from '@/api/images'
import { ElMessage, ElMessageBox } from 'element-plus'

const route = useRoute()
const router = useRouter()
const projectStore = useProjectStore()
const imageStore = useImageStore()

const projectId = computed(() => route.params.id as string)
const thumbnailSize = ref(180)
const filterStatus = ref<ReviewStatus | ''>('')
const filterMinRating = ref<number | null>(null)
const fileInputRef = ref<HTMLInputElement | null>(null)

// 统一选择集（用于导出和对比）
const selectedForExport = ref<Set<string>>(new Set())
// 全选标志
const isAllSelected = ref(false)

// 项目是否真正有图片（不受过滤影响）
const projectHasImages = computed(() => (projectStore.current?.imageCount ?? 0) > 0)

onMounted(async () => {
  await projectStore.fetchOne(projectId.value)
  await loadImages()
})

watch([filterStatus, filterMinRating], () => loadImages())

async function loadImages() {
  await imageStore.fetchList(projectId.value, {
    status: filterStatus.value === '' ? undefined : filterStatus.value,
    minRating: filterMinRating.value ?? undefined,
  })
  // 重置选择
  isAllSelected.value = false
  selectedForExport.value = new Set()
}

// 星级过滤选项
const ratingOptions = [
  { label: '全部', value: '' },
  { label: '≥1星', value: 1 },
  { label: '≥2星', value: 2 },
  { label: '≥3星', value: 3 },
  { label: '≥4星', value: 4 },
  { label: '5星', value: 5 },
  { label: '无星级', value: -1 },
]

// 上传处理
function triggerUpload() {
  fileInputRef.value?.click()
}
async function handleFileSelect(event: Event) {
  const input = event.target as HTMLInputElement
  if (!input.files?.length) return
  const files = Array.from(input.files)
  await imageStore.upload(projectId.value, files)
  input.value = ''
  await projectStore.fetchOne(projectId.value)
}

// 拖拽上传
const isDragging = ref(false)
function onDragOver(e: DragEvent) { e.preventDefault(); isDragging.value = true }
function onDragLeave() { isDragging.value = false }
async function onDrop(e: DragEvent) {
  e.preventDefault()
  isDragging.value = false
  const files = Array.from(e.dataTransfer?.files ?? []).filter(f => f.type.startsWith('image/'))
  if (files.length) await imageStore.upload(projectId.value, files)
  await projectStore.fetchOne(projectId.value)
}

// 快捷评分
async function quickRate(img: ImageItemDto, rating: number) {
  await imageStore.updateImage(img.id, { rating, status: img.status, notes: img.notes, tagsJson: img.tagsJson })
}

// 快捷状态切换
async function quickStatus(img: ImageItemDto, status: ReviewStatus) {
  await imageStore.updateImage(img.id, { rating: img.rating, status, notes: img.notes, tagsJson: img.tagsJson })
}

// 进入详情
function openDetail(img: ImageItemDto) {
  imageStore.setCurrentImage(img)
  router.push(`/image/${img.id}`)
}

// 统一选择/取消图片（导出/对比共用）
function toggleSelect(id: string) {
  const s = new Set(selectedForExport.value)
  if (s.has(id)) s.delete(id)
  else s.add(id)
  selectedForExport.value = s
  isAllSelected.value = s.size === imageStore.images.length && s.size > 0
}

// 全选 / 取消全选
function toggleSelectAll() {
  if (isAllSelected.value) {
    selectedForExport.value = new Set()
    isAllSelected.value = false
  } else {
    selectedForExport.value = new Set(imageStore.images.map(x => x.id))
    isAllSelected.value = true
  }
}

// 清空所有选择
function clearSelection() {
  selectedForExport.value = new Set()
  isAllSelected.value = false
}

// 对比：必须恰好选择2张
function goCompare() {
  const ids = Array.from(selectedForExport.value)
  if (ids.length !== 2) {
    ElMessage.warning('请选择 2 张图片进行对比')
    return
  }
  router.push({ path: `/projects/${projectId.value}/compare`, query: { ids: ids.join(',') } })
}

// 导出已选
const exporting = ref(false)
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL
async function exportSelected() {
  const ids = Array.from(selectedForExport.value)
  if (!ids.length) return ElMessage.warning('请先选择要导出的图片')
  exporting.value = true
  try {
    const res = await fetch(`${API_BASE_URL}/image/export`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ids }),
    })
    if (!res.ok) throw new Error('导出失败')
    const blob = await res.blob()
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `export_${Date.now()}.zip`
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } catch (e: any) {
    ElMessage.error(e.message || '导出失败')
  } finally {
    exporting.value = false
  }
}

// 导出项目全部图片（忽略当前选择）
async function exportAll() {
  exporting.value = true
  try {
    const res = await fetch(`${API_BASE_URL}/image/export`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ projectId: projectId.value }),
    })
    if (!res.ok) throw new Error('导出失败')
    const blob = await res.blob()
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `export_all_${Date.now()}.zip`
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success('全部导出成功')
  } catch (e: any) {
    ElMessage.error(e.message || '导出失败')
  } finally {
    exporting.value = false
  }
}

async function confirmDelete(img: ImageItemDto) {
  await ElMessageBox.confirm(`确认删除"${img.fileName}"？`, '删除', { type: 'warning' })
  await imageStore.deleteImage(img.id)
  // 删除后刷新列表并清理选择
  await loadImages()
}

function imgUrl(img: ImageItemDto) {
  return imageApi.getFileUrl(img.thumbnailPath || img.filePath)
}

function statusTag(s: ReviewStatus) {
  const map: Record<ReviewStatus, { label: string; type: string }> = {
    0: { label: '待评', type: 'info' },
    1: { label: '选中', type: 'success' },
    2: { label: '淘汰', type: 'danger' },
  }
  return map[s]
}

function formatSize(bytes: number) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1048576) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / 1048576).toFixed(1)} MB`
}
</script>

<template>
  <div class="project-detail">
    <!-- 顶栏 -->
    <div class="detail-header">
      <el-button text @click="router.push('/projects')">
        <el-icon><ArrowLeft /></el-icon>返回
      </el-button>
      <div class="header-title">
        <h2>{{ projectStore.current?.name }}</h2>
        <span class="count-badge">{{ imageStore.total }} 张</span>
      </div>
      <div class="header-actions">
        <!-- 对比按钮 -->
        <el-button v-if="selectedForExport.size === 2" type="primary" @click="goCompare">
          <el-icon><View /></el-icon>对比
        </el-button>
        <el-button @click="clearSelection" v-if="selectedForExport.size">取消选择</el-button>

        <!-- 导出下拉菜单 -->
        <el-dropdown v-if="imageStore.images.length > 0" trigger="click">
          <el-button :loading="exporting">
            <el-icon><Download /></el-icon>导出
          </el-button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item @click="toggleSelectAll">
                {{ isAllSelected ? '取消全选' : '全选当前图片' }}
              </el-dropdown-item>
              <el-dropdown-item @click="exportSelected" :disabled="selectedForExport.size === 0">
                导出已选 ({{ selectedForExport.size }})
              </el-dropdown-item>
              <el-dropdown-item @click="exportAll">导出项目全部图片</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>

        <!-- 上传按钮 -->
        <input ref="fileInputRef" type="file" multiple accept="image/*" @change="handleFileSelect" style="display:none" />
        <el-button type="primary" :loading="imageStore.uploading" @click="triggerUpload">
          <el-icon><Upload /></el-icon>
          {{ imageStore.uploading ? '上传中...' : '导入图片' }}
        </el-button>
      </div>
    </div>

    <!-- 过滤栏 -->
    <div class="filter-bar">
      <div class="filter-group">
        <span class="filter-label">状态：</span>
        <el-radio-group v-model="filterStatus" size="small">
          <el-radio-button value="">全部</el-radio-button>
          <el-radio-button :value="0">待评</el-radio-button>
          <el-radio-button :value="1">选中</el-radio-button>
          <el-radio-button :value="2">淘汰</el-radio-button>
        </el-radio-group>
      </div>
      <div class="filter-group">
        <span class="filter-label">星级：</span>
        <el-select v-model="filterMinRating" size="small" style="width: 110px" placeholder="全部">
          <el-option
            v-for="opt in ratingOptions"
            :key="String(opt.value)"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
      </div>
      <div class="filter-group" style="margin-left:auto">
        <span class="filter-label">缩略图大小：</span>
        <el-slider v-model="thumbnailSize" :min="120" :max="320" :step="20" style="width:120px" />
      </div>
    </div>

    <!-- 拖拽区提示：仅在项目完全无图片时显示 -->
    <div
      v-if="!projectHasImages && !imageStore.loading"
      class="drop-zone"
      :class="{ active: isDragging }"
      @dragover="onDragOver"
      @dragleave="onDragLeave"
      @drop="onDrop"
    >
      <el-icon size="48" color="#ccc"><UploadFilled /></el-icon>
      <p>拖拽图片到此处上传，或点击「导入图片」</p>
    </div>

    <!-- 过滤无结果提示 -->
    <el-empty
      v-if="projectHasImages && imageStore.images.length === 0 && !imageStore.loading"
      description="当前过滤条件下无图片"
    />

    <!-- 图片网格 -->
    <div
      v-if="imageStore.images.length > 0"
      class="image-grid"
      :style="{ '--thumb-size': thumbnailSize + 'px' }"
    >
      <div
        v-for="img in imageStore.images"
        :key="img.id"
        class="image-card"
        :class="{
          'export-selected': selectedForExport.has(img.id),
          'status-selected': img.status === 1,
          'status-rejected': img.status === 2,
        }"
      >
        <!-- 左上角统一选择圆圈 -->
        <div class="select-check" @click.stop="toggleSelect(img.id)">
          <el-icon v-if="selectedForExport.has(img.id)" style="color: #409eff"><Select /></el-icon>
          <el-icon v-else>
            <!-- <CirclePlus /> -->
          </el-icon>
        </div>

        <!-- 缩略图 -->
        <div class="thumb-wrap" @click="openDetail(img)">
          <img :src="imgUrl(img)" :alt="img.fileName" loading="lazy" />
          <el-tag class="status-tag" :type="statusTag(img.status).type" size="small">
            {{ statusTag(img.status).label }}
          </el-tag>
        </div>

        <!-- 元信息 -->
        <div class="image-info">
          <div class="info-filename" :title="img.fileName">{{ img.fileName }}</div>
          <div class="info-meta">{{ img.width }}×{{ img.height }} · {{ formatSize(img.fileSize) }}</div>
          <!-- 快捷评分 -->
          <el-rate
            :model-value="img.rating"
            :max="5"
            size="small"
            @change="(v: number) => quickRate(img, v)"
          />
          <!-- 快捷状态按钮 -->
          <div class="quick-actions">
            <el-button
              size="small"
              :type="img.status === 1 ? 'success' : ''"
              @click.stop="quickStatus(img, img.status === 1 ? 0 : 1)"
            >✓</el-button>
            <el-button
              size="small"
              :type="img.status === 2 ? 'danger' : ''"
              @click.stop="quickStatus(img, img.status === 2 ? 0 : 2)"
            >✗</el-button>
            <el-button size="small" text @click.stop="confirmDelete(img)">
              <el-icon><Delete /></el-icon>
            </el-button>
          </div>
        </div>
      </div>
    </div>

    <div v-if="imageStore.loading" class="loading-tip">
      <el-icon class="is-loading" size="24"><Loading /></el-icon> 加载中...
    </div>
  </div>
</template>

<style scoped>
.project-detail { max-width: 1400px; }

.detail-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
  flex-wrap: wrap;
}
.header-title {
  display: flex;
  align-items: center;
  gap: 8px;
  flex: 1;
}
.header-title h2 { font-size: 20px; font-weight: 600; }
.count-badge {
  background: #e8f4fd;
  color: #409eff;
  font-size: 12px;
  padding: 2px 8px;
  border-radius: 10px;
}
.header-actions { display: flex; gap: 8px; align-items: center; }

.filter-bar {
  display: flex;
  align-items: center;
  gap: 20px;
  padding: 12px 16px;
  background: #fff;
  border-radius: 8px;
  margin-bottom: 16px;
  flex-wrap: wrap;
}
.filter-group { display: flex; align-items: center; gap: 8px; }
.filter-label { font-size: 13px; color: #666; white-space: nowrap; }

.drop-zone {
  border: 2px dashed #ddd;
  border-radius: 10px;
  min-height: 200px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  color: #999;
  transition: border-color .2s, background .2s;
  margin-bottom: 12px;
}
.drop-zone.active { border-color: #409eff; background: #f0f8ff; }
.drop-zone p { font-size: 14px; }

.image-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(var(--thumb-size), 1fr));
  gap: 12px;
  padding: 4px;
}

.image-card {
  background: #fff;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0,0,0,.08);
  position: relative;
  transition: box-shadow .2s;
  border: 2px solid transparent;
}
.image-card:hover { box-shadow: 0 3px 12px rgba(0,0,0,.15); }
.image-card.export-selected { border-color: #e6a23c; }
.image-card.status-selected { border-color: #67c23a; }
.image-card.status-rejected { opacity: 0.55; }

.select-check {
  position: absolute;
  top: 6px;
  left: 6px;
  z-index: 2;
  background: rgba(255,255,255,.8);
  border-radius: 50%;
  width: 28px;
  height: 28px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;
}
.select-check:hover {
  background: rgba(255,255,255,1);
  transform: scale(1.05);
}
.select-check .el-icon {
  font-size: 20px;
}

.thumb-wrap {
  position: relative;
  aspect-ratio: 1;
  cursor: pointer;
  overflow: hidden;
  background: #f0f0f0;
}
.thumb-wrap img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform .2s;
}
.thumb-wrap:hover img { transform: scale(1.04); }
.status-tag {
  position: absolute;
  bottom: 4px;
  right: 4px;
}

.image-info { padding: 8px 10px; }
.info-filename {
  font-size: 12px;
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  margin-bottom: 2px;
}
.info-meta { font-size: 11px; color: #999; margin-bottom: 4px; }

.quick-actions {
  display: flex;
  gap: 4px;
  margin-top: 6px;
}
.quick-actions .el-button { padding: 2px 6px; min-height: unset; font-size: 13px; }

.loading-tip {
  text-align: center;
  padding: 32px;
  color: #999;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}
</style>