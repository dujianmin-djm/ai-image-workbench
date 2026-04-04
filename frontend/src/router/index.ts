import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      redirect: '/projects',
    },
    {
      path: '/projects',
      component: () => import('@/views/ProjectList.vue'),
      meta: { title: '项目列表' },
    },
    {
      path: '/projects/:id',
      component: () => import('@/views/ProjectDetail.vue'),
      meta: { title: '项目详情' },
    },
    {
      path: '/projects/:id/compare',
      component: () => import('@/views/Compare.vue'),
      meta: { title: '对比评审' },
    },
    {
      path: '/image/:id',
      component: () => import('@/views/ImageDetail.vue'),
      meta: { title: '图片详情' },
    },
  ],
})

export default router
