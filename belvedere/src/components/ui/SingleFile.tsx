"use client"

import {
  Dropzone,
  DropZoneArea,
  DropzoneMessage,
  DropzoneTrigger,
  useDropzone,
} from "@/components/ui/dropzone"
import { cn } from "@/lib/utils"
import { ImageIcon } from "@/components/ui/icons/ImageIcon.tsx"

export function SingleFile() {
  const dropzone = useDropzone({
    onDropFile: async (file: File) => {
      await new Promise((resolve) => setTimeout(resolve, 1000))
      return {
        status: "success",
        result: URL.createObjectURL(file),
      }
    },
    validation: {
      accept: {
        "image/*": [".png", ".jpg", ".jpeg"],
      },
      maxSize: 250 * 1024 * 1024,
      maxFiles: 1,
    },
    shiftOnMaxFiles: true,
  })

  const imageSrc = dropzone.fileStatuses[0]?.result
  const isPending = dropzone.fileStatuses[0]?.status === "pending"

  return (
    <Dropzone {...dropzone}>
      <div className="flex justify-between">
        <DropzoneMessage />
      </div>
      <DropZoneArea>
        <DropzoneTrigger className="flex gap-8 bg-transparent text-sm">
          <div
            className={cn(
              "relative flex h-20 w-20 items-center justify-center rounded-md border bg-background",
              isPending && "animate-pulse"
            )}
          >
            {imageSrc ? (
              <img
                src={imageSrc}
                alt="Uploaded"
                className="h-full w-full object-cover"
              />
            ) : (
              <ImageIcon className="size-8 text-muted-foreground" />
            )}
          </div>
          <div className="flex flex-col gap-1 font-semibold">
            <p>Upload a new photo</p>
          </div>
        </DropzoneTrigger>
      </DropZoneArea>
    </Dropzone>
  )
}
