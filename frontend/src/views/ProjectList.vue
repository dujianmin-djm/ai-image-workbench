<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useProjectStore } from '@/stores/projectStore'
import { imageApi } from '@/api/images'
import type { CreateUpdateProjectDto } from '@/api/projects'
import { ElMessage, ElMessageBox } from 'element-plus'

const router = useRouter()
const projectStore = useProjectStore()

const dialogVisible = ref(false)
const editingId = ref<string | null>(null)
const form = ref<CreateUpdateProjectDto>({ name: '', description: '', template: 'other' })

const templates = [
  { value: 'other', label: '通用' },
  { value: 'ai-gen', label: 'AI 生成图' },
  { value: 'heart-valve', label: '心脏瓣膜' },
  { value: 'design', label: '设计稿评审' },
  { value: 'ecommerce', label: '电商素材' },
]

const templateLabel = (t?: string) => templates.find((x) => x.value === t)?.label ?? '通用'

onMounted(() => {
  projectStore.fetchList()
})

function openCreate() {
  editingId.value = null
  form.value = { name: '', description: '', template: 'other' }
  dialogVisible.value = true
}

function openEdit(p: any) {
  editingId.value = p.id
  form.value = { name: p.name, description: p.description, template: p.template ?? 'other' }
  dialogVisible.value = true
}

async function submitForm() {
  if (!form.value.name.trim()) return ElMessage.warning('请输入项目名称')
  if (editingId.value) {
    await projectStore.updateProject(editingId.value, form.value)
    ElMessage.success('更新成功')
  } else {
    await projectStore.createProject(form.value)
    ElMessage.success('项目已创建')
  }
  dialogVisible.value = false
}

async function confirmDelete(id: string, name: string) {
  await ElMessageBox.confirm(`确认删除项目"${name}"？该操作将同时删除所有图片。`, '删除确认', {
    type: 'warning',
    confirmButtonText: '删除',
    cancelButtonText: '取消',
    confirmButtonClass: 'el-button--danger',
  })
  await projectStore.deleteProject(id)
  ElMessage.success('已删除')
}

function navigate(id: string) {
  router.push(`/projects/${id}`)
}

function getCoverUrl(path?: string | null) {
  return imageApi.getFileUrl(path)
}

function formatDate(s: string) {
  return new Date(s).toLocaleString('zh-CN')
}
</script>

<template>
  <div class="project-list">
    <div class="page-header">
      <h2>我的项目</h2>
      <el-button type="primary" @click="openCreate">
        <el-icon><Plus /></el-icon>新建项目
      </el-button>
    </div>

    <!-- 加载中 -->
    <div v-if="projectStore.loading" class="loading-container">
      <el-skeleton :rows="3" animated />
    </div>
    
    <!-- 加载完成但无数据 -->
    <el-empty 
      v-else-if="!projectStore.projects.length" 
      description="暂无项目，点击「新建项目」开始" 
    />
  
    <!-- 有数据时显示列表 -->
    <div v-else class="project-grid">
      <div
        v-for="p in projectStore.projects"
        :key="p.id"
        class="project-card"
        @click="navigate(p.id)"
      >
        <!-- 封面 -->
        <div class="card-cover">
          <img v-if="p.coverPath" :src="getCoverUrl(p.coverPath)" :alt="p.name" />
          <div v-else class="cover-placeholder">
            <el-icon size="40" color="#bbb"><Picture /></el-icon>
          </div>
          <span class="template-badge">{{ templateLabel(p.template) }}</span>
        </div>

        <!-- 信息 -->
        <div class="card-body">
          <div class="card-title">{{ p.name }}</div>
          <div class="card-meta">
            <span><el-icon><Files /></el-icon> {{ p.imageCount }} 张</span>
            <span>{{ formatDate(p.creationTime) }}</span>
          </div>
          <p v-if="p.description" class="card-desc">{{ p.description }}</p>
        </div>

        <!-- 操作 -->
        <div class="card-actions" @click.stop>
          <el-button text size="small" @click="openEdit(p)">
            <el-icon><Edit /></el-icon>编辑
          </el-button>
          <el-button text size="small" type="danger" @click="confirmDelete(p.id, p.name)">
            <el-icon><Delete /></el-icon>删除
          </el-button>
        </div>
      </div>
    </div>
  </div>

  <!-- 新建/编辑对话框 -->
  <el-dialog v-model="dialogVisible" :title="editingId ? '编辑项目' : '新建项目'" width="440px">
    <el-form label-width="80px">
      <el-form-item label="项目名称" required>
        <el-input v-model="form.name" placeholder="请输入项目名称" maxlength="128" show-word-limit />
      </el-form-item>
      <el-form-item label="类型">
        <el-select v-model="form.template" style="width:100%">
          <el-option v-for="t in templates" :key="t.value" :value="t.value" :label="t.label" />
        </el-select>
      </el-form-item>
      <el-form-item label="描述">
        <el-input v-model="form.description" type="textarea" :rows="3" placeholder="可选" maxlength="512" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="dialogVisible = false">取消</el-button>
      <el-button type="primary" @click="submitForm">确认</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.project-list { max-width: 1200px; }

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

.page-header h2 { font-size: 20px; font-weight: 600; }

.project-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 16px;
}

.project-card {
  background: #fff;
  border-radius: 10px;
  overflow: hidden;
  box-shadow: 0 1px 4px rgba(0,0,0,.08);
  cursor: pointer;
  transition: box-shadow .2s, transform .15s;
  display: flex;
  flex-direction: column;
  height: 100%; /* 确保卡片高度一致 */
}
.project-card:hover {
  box-shadow: 0 4px 16px rgba(0,0,0,.14);
  transform: translateY(-1px);
}

.card-cover {
  height: 160px;
  background: #f0f0f0;
  position: relative;
  overflow: hidden;
}
.card-cover img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  flex-shrink: 0; /* 防止封面被压缩 */
}
.cover-placeholder {
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}
.template-badge {
  position: absolute;
  top: 8px;
  right: 8px;
  background: rgba(0,0,0,.5);
  color: #fff;
  font-size: 11px;
  padding: 2px 8px;
  border-radius: 10px;
}

.card-body { 
  padding: 12px 14px 4px; 
  flex: 1; /* 占据剩余空间，将 actions 推到底部 */ 
}
.card-title { font-size: 15px; font-weight: 600; margin-bottom: 6px; }
.card-meta {
  font-size: 12px;
  color: #888;
  display: flex;
  gap: 12px;
  align-items: center;
}
.card-meta .el-icon { vertical-align: middle; margin-right: 2px; }
.card-desc {
  font-size: 12px;
  color: #999;
  margin-top: 6px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.card-actions {
  display: flex;
  gap: 4px;
  padding: 6px 10px 10px;
  border-top: 1px solid #f0f0f0;
  margin-top: 6px;
  flex-shrink: 0; /* 防止 actions 被压缩 */
}
</style>
