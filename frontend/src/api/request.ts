import axios from 'axios'
import { ElMessage } from 'element-plus'

const request = axios.create({
  baseURL: '/dapi',
  timeout: 30000,
})

request.interceptors.response.use(
  (response) => {
    const res = response.data
    if (res.code === '200') {
      return res.data
    } else {
      const errorMsg = res.message || '后端请求失败'
      ElMessage.error(errorMsg)
      console.error(errorMsg)
      return Promise.reject(new Error(errorMsg))
    }
  },
  (err) => {
    const msg = err.response?.data?.error?.message || err.message || '请求失败'
    ElMessage.error(msg)
    console.error(msg)
    return Promise.reject(new Error(msg))
  }
)

export default request
