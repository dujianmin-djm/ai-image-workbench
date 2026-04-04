<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useImageStore } from '@/stores/imageStore'
import { imageApi, type ReviewStatus } from '@/api/images'
import { ElMessage } from 'element-plus'

const route = useRoute()
const router = useRouter()
const imageStore = useImageStore()

const imgId = computed(() => route.params.id as string)
const img = computed(() => imageStore.currentImage)

const editRating = ref(0)
const editStatus = ref<ReviewStatus>(0)
const editNotes = ref('')
const editTagsRaw = ref('')
const saving = ref(false)

onMounted(async () => {
  if (!imageStore.currentImage || imageStore.currentImage.id !== imgId.value) {
    const found = await imageApi.get(imgId.value)
    imageStore.setCurrentImage(found)
  }
  syncForm()
  //window.addEventListener('keydown', onKeydown)
})

//onUnmounted(() => window.removeEventListener('keydown', onKeydown))

function syncForm() {
  if (!img.value) return
  editRating.value = img.value.rating
  editStatus.value = img.value.status
  editNotes.value = img.value.notes ?? ''
  editTagsRaw.value = parseTags(img.value.tagsJson).join(', ')
}

function parseTags(json?: string | null): string[] {
  if (!json) return []
  try { return JSON.parse(json) } catch { return [] }
}

async function save() {
  if (!img.value) return
  saving.value = true
  try {
    const tags = editTagsRaw.value
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean)
    await imageStore.updateImage(img.value.id, {
      rating: editRating.value,
      status: editStatus.value,
      notes: editNotes.value || undefined,
      tagsJson: tags.length ? JSON.stringify(tags) : undefined,
    })
    ElMessage.success('已保存')
  } finally {
    saving.value = false
  }
}

// 键盘快捷键
// function onKeydown(e: KeyboardEvent) {
//   if (e.key === 'Escape') router.back()
//   if (e.key === '1') setStatus(1)
//   if (e.key === '2') setStatus(2)
//   if (e.key === '0') setStatus(0)
// }

// async function setStatus(s: ReviewStatus) {
//   editStatus.value = s
//   await save()
// }

// 快速评审：选中或淘汰，保存后自动切换到下一张
async function quickReview(status: ReviewStatus) {
  if (!img.value || saving.value) return
  
  saving.value = true
  try {
    // 保存当前图片的评审状态
    const tags = editTagsRaw.value
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean)
    await imageStore.updateImage(img.value.id, {
      rating: editRating.value,
      status: status,
      notes: editNotes.value || undefined,
      tagsJson: tags.length ? JSON.stringify(tags) : undefined,
    })
    if (hasNext.value) {
      ElMessage.success(status === 1 ? '已选中，切换到下一张' : '已淘汰，切换到下一张')

      // 切换到下一张
      nextImg()
    } else {
      ElMessage.warning(status === 1 ? '已选中，当前已是最后一张' : '已淘汰，当前已是最后一张') 
    }
  } catch (error) {
    ElMessage.error('操作失败')
  } finally {
    saving.value = false
  }
}

function getImgUrl(path?: string | null) {
  return imageApi.getFileUrl(path)
}

function formatSize(bytes: number) {
  if (bytes < 1048576) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / 1048576).toFixed(1)} MB`
}

const statusOptions = [
  { value: 0, label: '待评', type: 'info' },
  { value: 1, label: '✓ 选中', type: 'success' },
  { value: 2, label: '✗ 淘汰', type: 'danger' },
]

function getRadioButtonStyle(opt: { value: number; label: string; type: string }) {
  if (editStatus.value === opt.value) {
    switch (opt.type) {
      case 'success':
        return { '--el-radio-button-checked-bg-color': '#67c23a', '--el-radio-button-checked-border-color': '#67c23a' }
      case 'danger':
        return { '--el-radio-button-checked-bg-color': '#f56c6c', '--el-radio-button-checked-border-color': '#f56c6c' }
      default:
        return {}
    }
  }
  return {}
}

function goBack() {
  if (img.value) router.push(`/projects/${img.value.projectId}`)
  else router.back()
}

// 在当前项目中前后切换
function prevImg() {
  const list = imageStore.images
  if (!list.length || !img.value) return
  const idx = list.findIndex((x) => x.id === img.value!.id)
  if (idx > 0) {
    imageStore.setCurrentImage(list[idx - 1])
    syncForm()
  }
}
function nextImg() {
  const list = imageStore.images
  if (!list.length || !img.value) return
  const idx = list.findIndex((x) => x.id === img.value!.id)
  if (idx < list.length - 1) {
    imageStore.setCurrentImage(list[idx + 1])
    syncForm()
  }
}

const hasPrev = computed(() => {
  const list = imageStore.images
  return list.findIndex((x) => x.id === img.value?.id) > 0
})
const hasNext = computed(() => {
  const list = imageStore.images
  const idx = list.findIndex((x) => x.id === img.value?.id)
  return idx >= 0 && idx < list.length - 1
})
</script>

<template>
  <div v-if="img" class="image-detail">
    <!-- 顶部导航 -->
    <div class="top-bar">
      <el-button text @click="goBack"><el-icon><ArrowLeft /></el-icon>返回</el-button>
      <span class="filename">{{ img.fileName }}</span>
      <div class="nav-btns">
        <el-button :disabled="!hasPrev" @click="prevImg"><el-icon><ArrowLeft /></el-icon></el-button>
        <el-button :disabled="!hasNext" @click="nextImg"><el-icon><ArrowRight /></el-icon></el-button>
      </div>
    </div>

    <div class="detail-body">
      <!-- 图片预览 -->
      <div class="preview-area">
        <img :src="getImgUrl(img.filePath)" :alt="img.fileName" class="main-image" />
      </div>

      <!-- 右侧面板 -->
      <div class="side-panel">
        <div class="panel-section">
          <h4>图片信息</h4>
          <div class="info-table">
            <div class="info-row"><span>尺寸</span><span>{{ img.width }} × {{ img.height }}</span></div>
            <div class="info-row"><span>大小</span><span>{{ formatSize(img.fileSize) }}</span></div>
            <div class="info-row"><span>格式</span><span>{{ img.mimeType }}</span></div>
            <div class="info-row"><span>文件名</span><span class="text-truncate">{{ img.fileName }}</span></div>
          </div>
        </div>

        <el-divider />

        <!-- 快速评审区域（绿色通道） -->
        <div class="panel-section quick-review">
          <div class="quick-header">
            <h4>快速评审</h4>
            <el-tooltip 
              content="点击后自动保存并切换下一张" 
              placement="top"
              :show-after="300"
            >
              <el-icon class="info-icon"><QuestionFilled /></el-icon>
            </el-tooltip>
          </div>
          <div class="quick-buttons">
            <el-button 
              type="success" 
              :loading="saving"
              @click="quickReview(1)"
              class="quick-btn select-btn"
            >
              <el-icon><Select /></el-icon> 选中
            </el-button>
            <el-button 
              type="danger" 
              :loading="saving"
              @click="quickReview(2)"
              class="quick-btn reject-btn"
            >
              <el-icon><CloseBold /></el-icon> 淘汰
            </el-button>
          </div>
        </div>

        <el-divider />

        <div class="panel-section">
          <!-- （键盘 1=选中 2=淘汰 0=重置） -->
          <h4>常规评审 <span class="hotkey-hint"></span></h4>

          <div class="action-group">
            <label>评分</label>
            <el-rate v-model="editRating" :max="5" />
          </div>

          <div class="action-group">
            <label>状态</label>
            <el-radio-group v-model="editStatus">
              <el-radio-button v-for="opt in statusOptions" :key="opt.value" :value="opt.value"
               :style="getRadioButtonStyle(opt)">
                {{ opt.label }}
              </el-radio-button>
            </el-radio-group>
          </div>

          <div class="action-group">
            <label>标签</label>
            <el-input v-model="editTagsRaw" placeholder="如：优秀, 人像, 户外" />
          </div>

          <div class="action-group">
            <label>备注 / 评审意见</label>
            <el-input v-model="editNotes" type="textarea" :rows="3" placeholder="记录选择或淘汰的理由" />
          </div>

          <el-button type="primary" :loading="saving" @click="save" style="width:100%;margin-top:8px">
            保存评审
          </el-button>
        </div>
      </div>
    </div>
  </div>

  <div v-else class="loading-tip">
    <el-icon class="is-loading" size="24"><Loading /></el-icon> 加载中...
  </div>
</template>

<style scoped>
.image-detail { height: calc(100vh - 40px); display: flex; flex-direction: column; }

.top-bar {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}
.filename {
  font-size: 14px;
  font-weight: 500;
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.nav-btns { display: flex; gap: 4px; }

.detail-body {
  flex: 1;
  display: flex;
  gap: 16px;
  overflow: hidden;
}

.preview-area {
  flex: 1;
  background: #1a1a1a;
  border-radius: 10px;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
}
.main-image {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
}

.side-panel {
  width: 280px;
  background: #fff;
  border-radius: 10px;
  padding: 16px;
  overflow-y: auto;
  flex-shrink: 0;
}

.panel-section h4 {
  font-size: 14px;
  font-weight: 600;
  margin-bottom: 12px;
  color: #333;
}
.hotkey-hint { font-size: 11px; font-weight: 400; color: #aaa; }

.info-table { display: flex; flex-direction: column; gap: 6px; }
.info-row {
  display: flex;
  justify-content: space-between;
  font-size: 13px;
}
.info-row span:first-child { color: #888; }
.text-truncate {
  max-width: 140px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.action-group { margin-bottom: 14px; }
.action-group label {
  display: block;
  font-size: 13px;
  color: #666;
  margin-bottom: 6px;
}

.loading-tip {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 80px;
  color: #999;
  gap: 8px;
}

/* 快速评审样式 */
.quick-review {
  background: #f0f9eb;
  border-radius: 8px;
  padding: 12px;
  margin-bottom: 8px;
}
.quick-header {
  display: flex;
  align-items: center;
  justify-content: left;
  margin-bottom: 12px;
}
.quick-header h4 {
  margin-bottom: 0;
  display: flex;
  align-items: center;
}
.info-icon {
  color: #8f9a8c;
  font-size: 16px;
  cursor: help;
  transition: color 0.2s;
  margin-left: 2px;
}
.info-icon:hover {
  color: #67c23a;
}
.quick-buttons {
  display: flex;
  gap: 12px;
}
.quick-btn {
  flex: 1;
  font-weight: 500;
}
.select-btn {
  background-color: #67c23a;
  border-color: #67c23a;
}
.select-btn:hover {
  background-color: #5daf34;
  border-color: #5daf34;
}
.reject-btn {
  background-color: #f56c6c;
  border-color: #f56c6c;
}
.reject-btn:hover {
  background-color: #e45656;
  border-color: #e45656;
}
</style>
