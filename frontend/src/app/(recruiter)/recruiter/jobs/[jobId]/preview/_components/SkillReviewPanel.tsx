"use client"

import { useState } from "react"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Check, X, HelpCircle, ChevronDown, Sparkles } from "lucide-react"
import type { JobSkillDecisionDto, SkillDecisionStatus } from "@/types/job-analysis.types"
import type { Skill } from "@/services/recruiter.service"

interface SkillReviewPanelProps {
  suggestions: JobSkillDecisionDto[]
  availableSkills: Skill[]
  onUpdateDecisions: (
    updated: Array<{
      decisionId: string
      decision: SkillDecisionStatus
      resolvedSkillId?: number | null
      importance: string
    }>
  ) => void
  isUpdating: boolean
}

export function SkillReviewPanel({
  suggestions,
  availableSkills,
  onUpdateDecisions,
  isUpdating,
}: SkillReviewPanelProps) {
  const [localDecisions, setLocalDecisions] = useState<Record<string, {
    decision: SkillDecisionStatus
    resolvedSkillId: number | null
    importance: string
  }>>({})

  const getEffectiveState = (item: JobSkillDecisionDto) => {
    const override = localDecisions[item.id]
    return {
      decision: override?.decision ?? item.decisionStatus,
      resolvedSkillId: override !== undefined ? override.resolvedSkillId : (item.resolvedSkillId ?? null),
      importance: override?.importance ?? item.importance,
    }
  }

  const handleDecisionChange = (
    id: string,
    item: JobSkillDecisionDto,
    newDecision: SkillDecisionStatus,
    newResolvedSkillId?: number | null,
    newImportance?: string
  ) => {
    const currentState = getEffectiveState(item)
    const updatedState: { decision: SkillDecisionStatus; resolvedSkillId: number | null; importance: string } = {
      decision: newDecision,
      resolvedSkillId: newResolvedSkillId !== undefined ? (newResolvedSkillId ?? null) : (currentState.resolvedSkillId ?? null),
      importance: newImportance ?? currentState.importance,
    }

    setLocalDecisions((prev) => ({
      ...prev,
      [id]: updatedState,
    }))
  }


  const handleSaveAll = () => {
    const payload = suggestions.map((item) => {
      const state = getEffectiveState(item)
      return {
        decisionId: item.id,
        decision: state.decision,
        resolvedSkillId: state.resolvedSkillId,
        importance: state.importance,
      }
    })

    onUpdateDecisions(payload)
  }

  if (suggestions.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Đề xuất kỹ năng AI</CardTitle>
          <CardDescription>Không có đề xuất kỹ năng nào được trích xuất.</CardDescription>
        </CardHeader>
      </Card>
    )
  }

  return (
    <Card className="border shadow-sm">
      <CardHeader className="bg-slate-50 border-b pb-4 flex flex-row items-center justify-between">
        <div>
          <CardTitle className="text-lg font-bold text-slate-900 flex items-center gap-2">
            <Sparkles className="w-5 h-5 text-indigo-600" />
            Đánh giá đề xuất kỹ năng ({suggestions.length})
          </CardTitle>
          <CardDescription>
            Kiểm tra và duyệt các kỹ năng do AI đề xuất trước khi xuất bản tin tuyển dụng.
          </CardDescription>
        </div>

        <Button
          onClick={handleSaveAll}
          disabled={isUpdating}
          className="bg-indigo-600 hover:bg-indigo-700 text-white"
        >
          Lưu quyết định duyệt
        </Button>
      </CardHeader>

      <CardContent className="p-0 divide-y">
        {suggestions.map((item) => {
          const state = getEffectiveState(item)
          const isAccepted = state.decision === "ACCEPTED"
          const isRejected = state.decision === "REJECTED"

          return (
            <div
              key={item.id}
              className={`p-4 transition-colors ${
                isAccepted
                  ? "bg-emerald-50/40"
                  : isRejected
                  ? "bg-red-50/40 opacity-70"
                  : "bg-white"
              }`}
            >
              <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                <div className="space-y-1.5 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="font-bold text-slate-900 text-base">
                      {item.rawMention}
                    </span>

                    {item.category && (
                      <Badge variant="outline" className="text-xs text-slate-600">
                        {item.category}
                      </Badge>
                    )}

                    <ResolutionBadge status={item.resolutionStatus} />

                    {item.confidence && (
                      <span className="text-xs text-slate-400">
                        Độ tin cậy: {Math.round(item.confidence * 100)}%
                      </span>
                    )}
                  </div>

                  {item.evidenceText && (
                    <p className="text-xs text-slate-600 italic bg-slate-100/70 p-2 rounded border border-slate-200">
                      "{item.evidenceText}"
                    </p>
                  )}

                  {/* Resolved Skill Selector if Ambiguous or Unmatched */}
                  {(item.resolutionStatus === "AMBIGUOUS" ||
                    item.resolutionStatus === "UNMATCHED") && (
                    <div className="pt-2 flex items-center gap-2">
                      <span className="text-xs text-amber-700 font-medium">
                        Chọn từ Điển kỹ năng chuẩn:
                      </span>
                      <select
                        value={state.resolvedSkillId ?? ""}
                        onChange={(e) => {
                          const val = e.target.value ? Number(e.target.value) : null
                          handleDecisionChange(item.id, item, state.decision, val)
                        }}
                        className="text-xs border rounded p-1 bg-white text-slate-800"
                      >
                        <option value="">-- Chưa gán (Giữ nguyên text gốc) --</option>
                        {availableSkills.map((sk) => (
                          <option key={sk.id} value={sk.id}>
                            {sk.name}
                          </option>
                        ))}
                      </select>
                    </div>
                  )}
                </div>

                {/* Controls */}
                <div className="flex flex-wrap items-center gap-2">
                  {/* Importance Selector */}
                  <select
                    value={state.importance}
                    onChange={(e) =>
                      handleDecisionChange(item.id, item, state.decision, undefined, e.target.value)
                    }
                    className="text-xs border rounded px-2 py-1.5 bg-white font-medium text-slate-700"
                  >
                    <option value="must_have">Must-have (Bắt buộc)</option>
                    <option value="nice_to_have">Nice-to-have (Ưu tiên)</option>
                  </select>

                  <Button
                    size="sm"
                    variant={isAccepted ? "default" : "outline"}
                    className={isAccepted ? "bg-emerald-600 hover:bg-emerald-700 text-white" : ""}
                    onClick={() => handleDecisionChange(item.id, item, "ACCEPTED")}
                  >
                    <Check className="w-4 h-4 mr-1" />
                    Chấp nhận
                  </Button>

                  <Button
                    size="sm"
                    variant={isRejected ? "destructive" : "outline"}
                    onClick={() => handleDecisionChange(item.id, item, "REJECTED")}
                  >
                    <X className="w-4 h-4 mr-1" />
                    Từ chối
                  </Button>
                </div>
              </div>
            </div>
          )
        })}
      </CardContent>
    </Card>
  )
}

function ResolutionBadge({ status }: { status: string }) {
  switch (status) {
    case "EXACT_CANONICAL":
      return <Badge className="bg-emerald-100 text-emerald-800 border-emerald-300">Khớp chuẩn (Canonical)</Badge>
    case "EXACT_ALIAS":
      return <Badge className="bg-blue-100 text-blue-800 border-blue-300">Khớp đồng nghĩa (Alias)</Badge>
    case "AMBIGUOUS":
      return <Badge className="bg-amber-100 text-amber-800 border-amber-300">Nhiều nghĩa (Ambiguous)</Badge>
    case "UNMATCHED":
      return <Badge className="bg-purple-100 text-purple-800 border-purple-300">Chưa có trong từ điển</Badge>
    default:
      return <Badge variant="secondary">{status}</Badge>
  }
}
