import request from './request'

export interface WorkProjectDto {
  id: string
  name: string
  description?: string
  template?: string
  coverPath?: string
  imageCount: number
  creationTime: string
}

export interface CreateUpdateProjectDto {
  name: string
  description?: string
  template?: string
}

export interface PagedRequest {
  current: number
  size: number
  sorts?: string
}

export interface PagedResponse<T> {
  total: number
  current: number
  size: number
  items: T[]
}

export const projectApi = {
  list: (params: PagedRequest) =>
    request.get<any, PagedResponse<WorkProjectDto>>('/project/query', { params }),

  get: (id: string) =>
    request.get<any, WorkProjectDto>(`/project/${id}`),

  create: (data: CreateUpdateProjectDto) =>
    request.post<any, WorkProjectDto>('/project/add', data),

  update: (id: string, data: CreateUpdateProjectDto) =>
    request.put<any, WorkProjectDto>(`/project/edit/${id}`, data),

  delete: (id: string) =>
    request.delete(`/project/delete/${id}`),
}
