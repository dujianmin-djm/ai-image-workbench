import request from './request'

export type ReviewStatus = 0 | 1 | 2  // Pending / Selected / Rejected

export interface ImageItemDto {
  id: string
  projectId: string
  fileName: string
  filePath: string
  thumbnailPath?: string
  fileSize: number
  width: number
  height: number
  mimeType?: string
  rating: number
  status: ReviewStatus
  notes?: string
  tagsJson?: string
  sortOrder: number
  creationTime: string
}

export interface UpdateImageItemDto {
  rating: number
  status: ReviewStatus
  notes?: string
  tagsJson?: string
}

export interface ImageQueryDto {
  projectId: string
  current: number
  size: number
  sorts?: string
  status?: ReviewStatus
  minRating?: number
  tag?: string
}

export const imageApi = {
  list: (params: ImageQueryDto) =>
    request.get<any, { total: number; items: ImageItemDto[]; current: number; size: number }>(
      '/image/query', { params }
    ),

  get: (id: string) =>
    request.get<any, ImageItemDto>(`/image/${id}`),

  upload: (projectId: string, files: File[]) => {
    const form = new FormData()
    files.forEach((f) => form.append('files', f))
    return request.post<any, ImageItemDto[]>(`/image/upload?projectId=${projectId}`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: 120000,
    })
  },

  update: (id: string, data: UpdateImageItemDto) =>
    request.put<any, ImageItemDto>(`/image/edit/${id}`, data),

  delete: (id: string) =>
    request.delete(`/image/delete/${id}`),

  deleteByProject: (projectId: string) =>
    request.delete(`/image/deleteByProject/${projectId}`),

  /** 获取静态资源完整 URL（走后端）*/
  getFileUrl: (path?: string | null) => normalizeFileUrl(path),
}

function normalizeFileUrl(path?: string | null) {
  if (!path) return ''
  if (/^https?:\/\//i.test(path)) return path
  return `${import.meta.env.VITE_BACKEND_ORIGIN}/${path.replace(/^\/+/, '')}`
}