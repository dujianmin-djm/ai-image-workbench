<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useImageStore } from '@/stores/imageStore'
import { imageApi, type ImageItemDto, type ReviewStatus } from '@/api/images'
import { ElMessage } from 'element-plus'

const route = useRoute()
const router = useRouter()
const imageStore = useImageStore()

const projectId = computed(() => route.params.id as string)
const ids = computed(() => (route.query.ids as string ?? '').split(',').filter(Boolean))

const leftImg = ref<ImageItemDto | null>(null)
const rightImg = ref<ImageItemDto | null>(null)
// const syncZoom = ref(true)

// 图片选择器
const selectMode = ref<'left' | 'right' | null>(null)
const allImages = computed(() => imageStore.images)

onMounted(async () => {
  if (!imageStore.images.length) {
    await imageStore.fetchList(projectId.value, { current: 1, size: 200 })
  }
  if (ids.value.length >= 2) {
    leftImg.value = allImages.value.find((x) => x.id === ids.value[0]) ?? null
    rightImg.value = allImages.value.find((x) => x.id === ids.value[1]) ?? null
  }
  if (!leftImg.value && allImages.value.length > 0) leftImg.value = allImages.value[0]
  if (!rightImg.value && allImages.value.length > 1) rightImg.value = allImages.value[1]
})

function selectImage(img: ImageItemDto) {
  if (selectMode.value === 'left') leftImg.value = img
  else if (selectMode.value === 'right') rightImg.value = img
  selectMode.value = null
}

async function setStatus(img: ImageItemDto | null, status: ReviewStatus) {
  if (!img) return
  await imageStore.updateImage(img.id, { rating: img.rating, status, notes: img.notes, tagsJson: img.tagsJson })
  // 同步更新本地引用
  if (leftImg.value?.id === img.id) leftImg.value = imageStore.images.find((x) => x.id === img.id) ?? leftImg.value
  if (rightImg.value?.id === img.id) rightImg.value = imageStore.images.find((x) => x.id === img.id) ?? rightImg.value
  ElMessage.success(`已标记为 ${status === 1 ? '选中' : status === 2 ? '淘汰' : '待评'}`)
}

async function setRating(img: ImageItemDto | null, rating: number) {
  if (!img) return
  await imageStore.updateImage(img.id, { rating, status: img.status, notes: img.notes, tagsJson: img.tagsJson })
}

function getUrl(img: ImageItemDto | null) {
  return imageApi.getFileUrl(img?.filePath)
}

function getThumbnail(img: ImageItemDto | null) {
  return imageApi.getFileUrl(img?.thumbnailPath || img?.filePath)
}

function formatSize(bytes: number) {
  if (bytes < 1048576) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / 1048576).toFixed(1)} MB`
}

const statusText: Record<ReviewStatus, string> = { 0: '待评', 1: '选中', 2: '淘汰' }
</script>

<template>
  <div class="compare-view">
    <!-- 顶栏 -->
    <div class="top-bar">
      <el-button text @click="router.push(`/projects/${projectId}`)">
        <el-icon><ArrowLeft /></el-icon>返回项目
      </el-button>
      <span class="title">双屏对比评审</span>
      <!-- <el-checkbox v-model="syncZoom" label="同步操作" /> -->
    </div>

    <!-- 对比主区 -->
    <div class="compare-body">

      <!-- 左侧 -->
      <div class="compare-pane">
        <div class="pane-header">
          <el-button size="small" @click="selectMode = 'left'">
            <el-icon><Switch /></el-icon> 切换图片
          </el-button>
          <span class="img-label">左侧</span>
        </div>
        <div class="pane-image">
          <img v-if="leftImg" :src="getUrl(leftImg)" :alt="leftImg.fileName" />
          <div v-else class="empty-pane">请选择图片</div>
        </div>
        <div v-if="leftImg" class="pane-meta">
          <div class="meta-row">
            <span class="mname">{{ leftImg.fileName }}</span>
            <el-tag :type="leftImg.status === 1 ? 'success' : leftImg.status === 2 ? 'danger' : 'info'" size="small">
              {{ statusText[leftImg.status] }}
            </el-tag>
          </div>
          <div class="meta-dims">{{ leftImg.width }}×{{ leftImg.height }} · {{ formatSize(leftImg.fileSize) }}</div>
          <el-rate :model-value="leftImg.rating" :max="5" size="small" @change="(v: number) => setRating(leftImg, v)" />
          <div class="verdict-btns">
            <el-button size="small" type="success" @click="setStatus(leftImg, 1)">✓ 选中</el-button>
            <el-button size="small" type="danger" @click="setStatus(leftImg, 2)">✗ 淘汰</el-button>
            <el-button size="small" @click="setStatus(leftImg, 0)">重置</el-button>
          </div>
        </div>
      </div>

      <!-- 分隔线 -->
      <div class="divider"><span>VS</span></div>

      <!-- 右侧 -->
      <div class="compare-pane">
        <div class="pane-header">
          <el-button size="small" @click="selectMode = 'right'">
            <el-icon><Switch /></el-icon> 切换图片
          </el-button>
          <span class="img-label">右侧</span>
        </div>
        <div class="pane-image">
          <img v-if="rightImg" :src="getUrl(rightImg)" :alt="rightImg.fileName" />
          <div v-else class="empty-pane">请选择图片</div>
        </div>
        <div v-if="rightImg" class="pane-meta">
          <div class="meta-row">
            <span class="mname">{{ rightImg.fileName }}</span>
            <el-tag :type="rightImg.status === 1 ? 'success' : rightImg.status === 2 ? 'danger' : 'info'" size="small">
              {{ statusText[rightImg.status] }}
            </el-tag>
          </div>
          <div class="meta-dims">{{ rightImg.width }}×{{ rightImg.height }} · {{ formatSize(rightImg.fileSize) }}</div>
          <el-rate :model-value="rightImg.rating" :max="5" size="small" @change="(v: number) => setRating(rightImg, v)" />
          <div class="verdict-btns">
            <el-button size="small" type="success" @click="setStatus(rightImg, 1)">✓ 选中</el-button>
            <el-button size="small" type="danger" @click="setStatus(rightImg, 2)">✗ 淘汰</el-button>
            <el-button size="small" @click="setStatus(rightImg, 0)">重置</el-button>
          </div>
        </div>
      </div>
    </div>

    <!-- 底部缩略图选择器 -->
    <div v-if="selectMode" class="thumb-picker">
      <div class="picker-header">
        选择{{ selectMode === 'left' ? '左侧' : '右侧' }}图片
        <el-button text size="small" @click="selectMode = null"><el-icon><Close /></el-icon></el-button>
      </div>
      <div class="thumb-list">
        <div
          v-for="img in allImages"
          :key="img.id"
          class="thumb-item"
          :class="{
            'active-left': leftImg?.id === img.id,
            'active-right': rightImg?.id === img.id,
          }"
          @click="selectImage(img)"
        >
          <img :src="getThumbnail(img)" :alt="img.fileName" />
          <el-tag
            v-if="img.status !== 0"
            :type="img.status === 1 ? 'success' : 'danger'"
            size="small"
            class="t-tag"
          >{{ statusText[img.status] }}</el-tag>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.compare-view {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 40px);
  gap: 8px;
}

.top-bar {
  display: flex;
  align-items: center;
  gap: 12px;
}
.title { font-size: 16px; font-weight: 600; flex: 1; }

.compare-body {
  flex: 1;
  display: flex;
  gap: 0;
  overflow: hidden;
  background: #111;
  border-radius: 10px;
}

.compare-pane {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.pane-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 12px;
  background: #1e1e2e;
  color: #ccc;
}
.img-label { font-size: 12px; color: #888; }

.pane-image {
  flex: 1;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #0d0d0d;
}
.pane-image img {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
}
.empty-pane {
  color: #555;
  font-size: 16px;
}

.pane-meta {
  background: #1e1e2e;
  padding: 10px 14px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.meta-row { display: flex; align-items: center; gap: 8px; }
.mname {
  font-size: 13px;
  color: #ddd;
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.meta-dims { font-size: 12px; color: #777; }

.verdict-btns { display: flex; gap: 6px; }

.divider {
  width: 40px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #0d0d0d;
}
.divider span {
  writing-mode: vertical-rl;
  color: #444;
  font-size: 13px;
  letter-spacing: 4px;
}

.thumb-picker {
  background: #fff;
  border-radius: 8px;
  max-height: 160px;
  flex-shrink: 0;
}
.picker-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 14px;
  font-size: 13px;
  font-weight: 500;
  border-bottom: 1px solid #eee;
}
.thumb-list {
  display: flex;
  gap: 8px;
  padding: 10px 14px;
  overflow-x: auto;
}
.thumb-item {
  width: 90px;
  height: 90px;
  flex-shrink: 0;
  border-radius: 6px;
  overflow: hidden;
  cursor: pointer;
  border: 2px solid transparent;
  position: relative;
}
.thumb-item:hover { border-color: #aaa; }
.thumb-item.active-left { border-color: #409eff; }
.thumb-item.active-right { border-color: #e6a23c; }
.thumb-item img { width: 100%; height: 100%; object-fit: cover; }
.t-tag { position: absolute; bottom: 2px; right: 2px; }
</style>
