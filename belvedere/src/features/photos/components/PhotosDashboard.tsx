import { Button } from "@/components/ui/button"
import { Field } from "@/components/ui/field.tsx"
import { Input } from "@/components/ui/input.tsx"
import { Masonry } from "@/components/ui/masonry.tsx"

import { AddPhotoDialog } from "@/features/photos/components/AddPhotoDialog.tsx"

export function PhotosDashboard() {
  return (
    <main className="space-y-6 p-6">
      <div className="flex items-center justify-between gap-9 md:gap-x-24 lg:gap-x-48">
        <Field orientation="horizontal">
          <Input type="search" placeholder="Search..." />
          <Button>Search</Button>
        </Field>

        <AddPhotoDialog/>
      </div>
      <Masonry photos={[]} />
    </main>
  )
}
