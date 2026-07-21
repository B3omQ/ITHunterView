"use client"

import * as React from "react"
import { Check, ChevronsUpDown } from "lucide-react"

import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command"
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover"

interface Major {
  id: number;
  name: string;
  parentId?: number;
  parentName?: string;
}

interface MajorComboboxProps {
  majors: Major[]
  value: string
  onChange: (value: string) => void
  className?: string
  placeholder?: string
}

export function MajorCombobox({ majors, value, onChange, className, placeholder = "Select expertise..." }: MajorComboboxProps) {
  const [open, setOpen] = React.useState(false)

  const roots = majors.filter(m => !m.parentId);
  const getChildren = (parentId: number) => majors.filter(m => m.parentId === parentId);
  const orphans = majors.filter(m => m.parentId && !roots.some(r => r.id === m.parentId));

  const handleSelect = (currentValue: string) => {
    const selected = majors.find(m => m.name.toLowerCase() === currentValue.toLowerCase())?.name || currentValue;
    onChange(selected === value ? "" : selected)
    setOpen(false)
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger
        render={
          <Button
            variant="outline"
            role="combobox"
            aria-expanded={open}
            className={cn("w-full justify-between font-normal bg-white dark:bg-zinc-950 border-zinc-200 dark:border-zinc-800 transition-all", className)}
          >
            <span className="truncate">
              {value
                ? majors.find((major) => major.name === value)?.name || value
                : placeholder}
            </span>
            <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
          </Button>
        }
      />
      <PopoverContent className="w-full min-w-[300px] p-0" align="start">
        <Command>
          <CommandInput placeholder="Search expertise..." />
          <CommandList className="max-h-[300px] overflow-y-auto">
            <CommandEmpty>No expertise found.</CommandEmpty>
            <CommandGroup>
              {roots.map((root) => (
                <React.Fragment key={root.id}>
                  <CommandItem
                    value={root.name}
                    onSelect={handleSelect}
                    className="font-semibold bg-zinc-50 dark:bg-zinc-900/50"
                  >
                    <Check
                      className={cn(
                        "mr-2 h-4 w-4",
                        value === root.name ? "opacity-100" : "opacity-0"
                      )}
                    />
                    {root.name}
                  </CommandItem>
                  
                  {getChildren(root.id).map(child => (
                    <CommandItem
                      key={child.id}
                      value={child.name}
                      onSelect={handleSelect}
                      className="pl-8"
                    >
                      <Check
                        className={cn(
                          "mr-2 h-4 w-4",
                          value === child.name ? "opacity-100" : "opacity-0"
                        )}
                      />
                      {child.name}
                    </CommandItem>
                  ))}
                </React.Fragment>
              ))}

              {orphans.map(orphan => (
                  <CommandItem
                    key={orphan.id}
                    value={orphan.name}
                    onSelect={handleSelect}
                  >
                    <Check
                      className={cn(
                        "mr-2 h-4 w-4",
                        value === orphan.name ? "opacity-100" : "opacity-0"
                      )}
                    />
                    {orphan.name}
                  </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  )
}
