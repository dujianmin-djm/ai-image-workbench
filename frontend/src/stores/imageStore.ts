import { defineStore } from 'pinia'
import { ref } from 'vue'
import { imageApi, type ImageItemDto, type UpdateImageItemDto, type ImageQueryDto } from '@/api/images'
import { ElMessage } from 'element-plus'

export const useImageStore = defineStore('image', () => {
  const images = ref<ImageItemDto[]>([])
  const total = ref(0)
  const loading = ref(false)
  const uploading = ref(false)
  const currentImage = ref<ImageItemDto | null>(null)

  // 当前项目检索参数
  const query = ref<ImageQueryDto>({
    projectId: '',
    current: 1,
    size: 50,
    sorts: 'SortOrder asc, CreationTime asc',
  })

  async function fetchList(projectId: string, params?: Partial<ImageQueryDto>) {
    query.value = { ...query.value, projectId, ...params, current: params?.current ?? 1 }
    loading.value = true
    try {
      const res = await imageApi.list(query.value)
      images.value = res.items
      total.value = res.total
    } catch (e: any) {
      ElMessage.error(e.message)
    } finally {
      loading.value = false
    }
  }

  async function loadMore(projectId: string) {
    const nextPage = query.value.current + 1
    if (images.value.length >= total.value) return
    loading.value = true
    try {
      const res = await imageApi.list({ ...query.value, projectId, current: nextPage })
      images.value = [...images.value, ...res.items]
      query.value.current = nextPage
    } finally {
      loading.value = false
    }
  }

  async function upload(projectId: string, files: File[]) {
    uploading.value = true
    try {
      const result = await imageApi.upload(projectId, files)
      await fetchList(projectId)
      return result
    } catch (e: any) {
      ElMessage.error(e.message)
      return []
    } finally {
      uploading.value = false
    }
  }

  async function updateImage(id: string, data: UpdateImageItemDto) {
    const res = await imageApi.update(id, data)
    const idx = images.value.findIndex((x) => x.id === id)
    if (idx >= 0) images.value[idx] = res
    if (currentImage.value?.id === id) currentImage.value = res
    return res
  }

  async function deleteImage(id: string) {
    await imageApi.delete(id)
    images.value = images.value.filter((x) => x.id !== id)
    total.value = Math.max(0, total.value - 1)
  }

  function setCurrentImage(img: ImageItemDto | null) {
    currentImage.value = img
  }

  return {
    images, total, loading, uploading, currentImage, query,
    fetchList, loadMore, upload, updateImage, deleteImage, setCurrentImage
  }
})
