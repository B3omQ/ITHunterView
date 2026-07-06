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
import { VIETNAM_PROVINCES } from "@/lib/job-constants"

const locations = [
  ...VIETNAM_PROVINCES.map(p => ({ value: p, label: p })),
  { value: "Other", label: "Other" }
]

interface LocationComboboxProps {
  value: string
  onChange: (value: string) => void
  className?: string
}

export function LocationCombobox({ value, onChange, className }: LocationComboboxProps) {
  const [open, setOpen] = React.useState(false)

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger
        render={
          <Button
            variant="outline"
            role="combobox"
            aria-expanded={open}
            className={cn("justify-between font-normal bg-background/50 border-border/60 hover:bg-background/80 transition-all", className)}
          >
            {value
              ? locations.find((loc) => loc.value === value)?.label || value
              : "Select location..."}
            <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
          </Button>
        }
      />
      <PopoverContent className="w-[300px] p-0" align="start">
        <Command>
          <CommandInput placeholder="Search location..." />
          <CommandList>
            <CommandEmpty>No location found.</CommandEmpty>
            <CommandGroup>
              {locations.map((loc) => (
                <CommandItem
                  key={loc.value}
                  value={loc.value}
                  onSelect={(currentValue) => {
                    const selected = locations.find(l => l.value.toLowerCase() === currentValue)?.value || currentValue;
                    onChange(selected === value ? "" : selected)
                    setOpen(false)
                  }}
                >
                  <Check
                    className={cn(
                      "mr-2 h-4 w-4",
                      value === loc.value ? "opacity-100" : "opacity-0"
                    )}
                  />
                  {loc.label}
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  )
}
