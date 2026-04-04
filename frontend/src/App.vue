<script setup lang="ts">
import { useRoute } from 'vue-router'

const route = useRoute()
</script>

<template>
  <el-container class="app-root">
    <!-- 侧边栏 -->
    <el-aside width="200px" class="sidebar">
      <div class="sidebar-logo">
        <el-icon size="22"><Picture /></el-icon>
        <span>图片工作台</span>
      </div>
      <el-menu
        router
        :default-active="route.path"
        background-color="#1a1a2e"
        text-color="#ccc"
        active-text-color="#409eff"
      >
        <el-menu-item index="/projects">
          <el-icon><FolderOpened /></el-icon>
          <span>我的项目</span>
        </el-menu-item>
      </el-menu>
    </el-aside>

    <!-- 主内容区 -->
    <el-container>
      <el-main class="main-content">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<style>
* { box-sizing: border-box; margin: 0; padding: 0; }
html, body, #app { height: 100%; }

.app-root {
  height: 100vh;
  overflow: hidden;
}

.sidebar {
  background: #1a1a2e;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.sidebar-logo {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 18px 20px;
  color: #fff;
  font-weight: 600;
  font-size: 15px;
  border-bottom: 1px solid #2a2a4a;
}

.sidebar .el-menu {
  border-right: none;
  flex: 1;
}

.main-content {
  padding: 20px;
  overflow-y: auto;
  background: #f5f5f5;
  height: 100vh;
}
</style>
