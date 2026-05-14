import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog.tsx"
import { Button } from "@/components/ui/button.tsx"
import { UploadIcon } from "@/components/ui/icons/UploadIcon.tsx"
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from "@/components/ui/tabs.tsx"
import { Label } from "@/components/ui/label.tsx"
import { Input } from "@/components/ui/input.tsx"
import { Textarea } from "@/components/ui/textarea.tsx"
import { MultiImages } from "@/components/ui/MultiImages.tsx"
import { SingleFile } from "@/components/ui/SingleFile.tsx"

export function AddPhotoDialog() {
  return (
    <Dialog>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm">
          <UploadIcon /> Upload new
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Upload Photos</DialogTitle>
          <DialogDescription>
            Upload single or multiple photos.
          </DialogDescription>
        </DialogHeader>
        <Tabs defaultValue="multiple" className="w-full">
          <TabsList className="grid w-full grid-cols-2">
            <TabsTrigger value="multiple">Multiple</TabsTrigger>
            <TabsTrigger value="single">Single</TabsTrigger>
          </TabsList>
          <TabsContent value="multiple" className="mt-4">
            <MultiImages
              onDropFile={function (file: File): void {
                console.log(file);
                throw new Error("Function not implemented.")
              }}
              acceptedTypes={[".png", ".jpg", ".jpeg"]}
              maxSize={100 * 1024 * 1024}
              maxFiles={100}
            />
          </TabsContent>
          <TabsContent value="single" className="mt-4 space-y-4">
            <SingleFile />
            <div className="space-y-2">
              <Label htmlFor="title">Title</Label>
              <Input id="title" placeholder="Enter a title" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Textarea id="description" placeholder="Enter a description" />
            </div>
          </TabsContent>
        </Tabs>
        <DialogFooter>
          <Button type="submit">Upload</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
