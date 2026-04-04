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
const filterMinRating = ref(0)

// 对比选择（最多2张）
const selectedForCompare = ref<string[]>([])

onMounted(async () => {
  await projectStore.fetchOne(projectId.value)
  await loadImages()
})

watch([filterStatus, filterMinRating], () => loadImages())

async function loadImages() {
  await imageStore.fetchList(projectId.value, {
    status: filterStatus.value === '' ? undefined : filterStatus.value,
    minRating: filterMinRating.value || undefined,
  })
}

// 上传处理
async function handleFileSelect(event: Event) {
  const input = event.target as HTMLInputElement
  if (!input.files?.length) return
  const files = Array.from(input.files)
  await imageStore.upload(projectId.value, files)
  input.value = ''
  // 刷新项目信息
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

// 对比选择
function toggleCompare(id: string) {
  if (selectedForCompare.value.includes(id)) {
    selectedForCompare.value = selectedForCompare.value.filter((x) => x !== id)
  } else if (selectedForCompare.value.length < 2) {
    selectedForCompare.value = [...selectedForCompare.value, id]
  } else {
    ElMessage.warning('最多同时选择 2 张进行对比')
  }
}

function goCompare() {
  router.push({ path: `/projects/${projectId.value}/compare`, query: { ids: selectedForCompare.value.join(',') } })
}

async function confirmDelete(img: ImageItemDto) {
  await ElMessageBox.confirm(`确认删除"${img.fileName}"？`, '删除', { type: 'warning' })
  await imageStore.deleteImage(img.id)
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
        <el-button
          v-if="selectedForCompare.length === 2"
          type="primary"
          @click="goCompare"
        >
          <el-icon><View /></el-icon>对比 ({{ selectedForCompare.length }})
        </el-button>
        <el-button @click="selectedForCompare = []" v-if="selectedForCompare.length">取消选择</el-button>

        <!-- 上传按钮 -->
        <label class="upload-btn">
          <el-button type="primary" :loading="imageStore.uploading">
            <el-icon><Upload /></el-icon>
            {{ imageStore.uploading ? '上传中...' : '导入图片' }}
          </el-button>
          <input type="file" multiple accept="image/*" @change="handleFileSelect" style="display:none" />
        </label>
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
        <span class="filter-label">最低星级：</span>
        <el-rate v-model="filterMinRating" :max="5" size="small" />
      </div>
      <div class="filter-group" style="margin-left:auto">
        <span class="filter-label">缩略图大小：</span>
        <el-slider v-model="thumbnailSize" :min="120" :max="320" :step="20" style="width:120px" />
      </div>
    </div>

    <!-- 拖拽区提示 -->
    <div
      v-if="imageStore.images.length === 0 && !imageStore.loading"
      class="drop-zone"
      :class="{ active: isDragging }"
      @dragover="onDragOver"
      @dragleave="onDragLeave"
      @drop="onDrop"
    >
      <el-icon size="48" color="#ccc"><UploadFilled /></el-icon>
      <p>拖拽图片到此处上传，或点击「导入图片」</p>
    </div>

    <!-- 图片网格 -->
    <div
      v-if="imageStore.images.length > 0"
      class="image-grid"
      :style="{ '--thumb-size': thumbnailSize + 'px' }"
      @dragover="onDragOver"
      @dragleave="onDragLeave"
      @drop="onDrop"
    >
      <div
        v-for="img in imageStore.images"
        :key="img.id"
        class="image-card"
        :class="{
          selected: selectedForCompare.includes(img.id),
          'status-selected': img.status === 1,
          'status-rejected': img.status === 2,
        }"
      >
        <!-- 选框 -->
        <div class="select-check" @click.stop="toggleCompare(img.id)">
          <el-icon v-if="selectedForCompare.includes(img.id)" style="color:blue"><Select /></el-icon>
          <el-icon v-else><Circle /></el-icon>
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
          <!-- 快捷状态 -->
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
.upload-btn { cursor: pointer; }

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
.image-card.selected { border-color: #409eff; }
.image-card.status-selected { border-color: #67c23a; }
.image-card.status-rejected { opacity: 0.55; }

.select-check {
  position: absolute;
  top: 6px;
  left: 6px;
  z-index: 2;
  background: rgba(255,255,255,.8);
  border-radius: 50%;
  padding: 8px;
  cursor: pointer;
  display: flex;
  align-items: center;
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
