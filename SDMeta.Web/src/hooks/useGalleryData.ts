import { useInfiniteQuery, useMutation, useQuery, useQueryClient, type UseInfiniteQueryResult } from '@tanstack/react-query'
import { api } from '../lib/api'
import type { GalleryQueryState, ImageListResponse } from '../types/api'

export interface UseGalleryDataOptions {
  queryState: GalleryQueryState
  selectedImageId: string | null
  isSettingsOpen: boolean
  autoRescan: boolean
  onClearSelection: () => void
}

export function useGalleryData({
  queryState,
  selectedImageId,
  isSettingsOpen,
  autoRescan,
  onClearSelection,
}: UseGalleryDataOptions) {
  const queryClient = useQueryClient()

  const imageQuery = useInfiniteQuery({
    queryKey: ['images', queryState],
    queryFn: ({ pageParam }) => api.getImages(queryState, pageParam, 100),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage: ImageListResponse) => lastPage.nextCursor ?? undefined,
  }) as UseInfiniteQueryResult<ImageListResponse, Error>

  const modelsQuery = useQuery({
    queryKey: ['models'],
    queryFn: api.getModels,
  })

  const pendingQuery = useQuery({
    queryKey: ['pending-changes'],
    queryFn: api.getPendingChanges,
    refetchInterval: autoRescan ? 2000 : false,
  })

  const storageQuery = useQuery({
    queryKey: ['storage'],
    queryFn: api.getStorage,
    enabled: isSettingsOpen,
  })

  const detailQuery = useQuery({
    queryKey: ['image-detail', selectedImageId],
    queryFn: () => api.getImageDetail(selectedImageId!),
    enabled: selectedImageId !== null,
  })

  const clearThumbsMutation = useMutation({
    mutationFn: api.clearThumbnails,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['storage'] }),
  })

  const clearDbMutation = useMutation({
    mutationFn: api.clearDatabase,
    onSuccess: async () => {
      onClearSelection()
      await queryClient.invalidateQueries({ queryKey: ['images'] })
      await queryClient.invalidateQueries({ queryKey: ['storage'] })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: api.deleteImage,
    onSuccess: async () => {
      onClearSelection()
      await queryClient.invalidateQueries({ queryKey: ['images'] })
      await queryClient.invalidateQueries({ queryKey: ['pending-changes'] })
    },
  })

  const invalidateAfterScan = async () => {
    await queryClient.invalidateQueries({ queryKey: ['images'] })
    await queryClient.invalidateQueries({ queryKey: ['pending-changes'] })
  }

  return {
    imageQuery,
    modelsQuery,
    pendingQuery,
    storageQuery,
    detailQuery,
    clearThumbsMutation,
    clearDbMutation,
    deleteMutation,
    invalidateAfterScan,
  }
}
