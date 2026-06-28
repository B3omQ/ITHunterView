# Frontend Design Rules

This project uses shadcn/ui as the design system. Do not invent a new visual
system, component library, token set, or styling language.

## Priority

1. Use pure shadcn/ui components first.
2. Check the existing `ui` folder for shadcn/ui components before considering
   any new type of custom component.
3. Compose screens from existing project components when available.
4. Add custom UI only when shadcn/ui and existing `ui` components do not provide the needed component or
   behavior.

## shadcn Usage

- Do not customize shadcn/ui components unless the task explicitly requires it.
- Do not restyle shadcn/ui components to create a new brand direction.
- Keep the default shadcn interaction patterns, spacing, radius, variants, and
  accessibility behavior.
- Prefer the documented shadcn component API over custom wrappers.
- Use Tailwind utilities only for layout, spacing between components, and minor
  responsive adjustments.

## Visual Direction

- Keep the UI calm, neutral, and readable.
- Prefer the existing neutral/zinc palette.
- Prefer borders over shadows.
- Avoid gradients, glassmorphism, decorative backgrounds, and ornamental effects.
- Keep spacing consistent with the existing application.

## Wireframes

Wireframes describe what should exist on the screen. They are not the source of
truth for visual styling, component implementation, spacing, colors, or exact
layout details.

When a wireframe conflicts with shadcn/ui or existing project components, follow
the design system and preserve the intended content and behavior from the
wireframe.
