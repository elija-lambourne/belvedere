import { Navigate, Route, Routes } from "react-router-dom"

import { AlbumsPage } from "@/features/albums"
import { LoginPage } from "@/features/auth"
import { PhotosDashboard } from "@/features/photos"

import { ProtectedRoute } from "./ProtectedRoute"

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <PhotosDashboard />
          </ProtectedRoute>
        }
      />
      <Route
        path="/albums"
        element={
          <ProtectedRoute>
            <AlbumsPage />
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<Navigate replace to="/" />} />
    </Routes>
  )
}

