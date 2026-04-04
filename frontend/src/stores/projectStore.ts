import { defineStore } from 'pinia'
import { ref } from 'vue'
import { projectApi, type WorkProjectDto, type CreateUpdateProjectDto } from '@/api/projects'
import { ElMessage } from 'element-plus'

export const useProjectStore = defineStore('project', () => {
  const projects = ref<WorkProjectDto[]>([])
  const total = ref(0)
  const loading = ref(false)
  const current = ref<WorkProjectDto | null>(null)

  async function fetchList(page = 1, size = 20) {
    loading.value = true
    try {
      const res = await projectApi.list({ current: page, size, sorts: 'CreationTime desc' })
      projects.value = res.items
      total.value = res.total
    } catch (e: any) {
      ElMessage.error(e.message)
    } finally {
      loading.value = false
    }
  }

  async function fetchOne(id: string) {
    const res = await projectApi.get(id)
    current.value = res
    return res
  }

  async function createProject(data: CreateUpdateProjectDto) {
    const res = await projectApi.create(data)
    await fetchList()
    return res
  }

  async function updateProject(id: string, data: CreateUpdateProjectDto) {
    const res = await projectApi.update(id, data)
    await fetchList()
    return res
  }

  async function deleteProject(id: string) {
    await projectApi.delete(id)
    await fetchList()
  }

  return { projects, total, loading, current, fetchList, fetchOne, createProject, updateProject, deleteProject }
})
