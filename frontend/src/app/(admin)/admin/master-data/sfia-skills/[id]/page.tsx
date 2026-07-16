"use client";

import React, { use } from "react";
import { useRouter } from "next/navigation";
import { ArrowLeft, AlertCircle, Bookmark, Layers, FileText } from "lucide-react";
import { useSfiaSkill } from "@/hooks/useSfiaSkill";

export default function SfiaSkillDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const router = useRouter();
  const { id } = use(params);

  const { data, isLoading, isError } = useSfiaSkill(id);
  const skill = data?.data;

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center h-[50vh] text-muted-foreground">
        <div className="w-8 h-8 border-4 border-primary border-t-transparent rounded-full animate-spin mb-4"></div>
        <p className="text-sm">Loading skill details...</p>
      </div>
    );
  }

  if (isError || !skill) {
    return (
      <div className="flex flex-col items-center justify-center h-[50vh] text-destructive space-y-4">
        <AlertCircle size={40} className="opacity-50" />
        <div className="text-center">
          <p className="text-base font-semibold">Skill not found</p>
          <p className="text-sm opacity-80 mt-1">The requested SFIA skill could not be loaded.</p>
        </div>
        <button
          onClick={() => router.push("/admin/master-data/sfia-skills")}
          className="px-4 py-2 bg-muted text-foreground hover:bg-muted/80 rounded-xl text-sm font-medium transition-colors"
        >
          Go Back
        </button>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full bg-background animate-in fade-in duration-300">
      <div className="flex items-center gap-4 px-6 py-4 border-b border-border bg-card sticky top-0 z-10">
        <button
          onClick={() => router.push("/admin/master-data/sfia-skills")}
          className="p-2 text-muted-foreground hover:text-foreground hover:bg-muted rounded-xl transition-colors"
        >
          <ArrowLeft size={20} />
        </button>
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground flex items-center gap-3">
            {skill.skillName}
            <span className="text-sm font-mono font-medium px-2 py-0.5 bg-primary/10 text-primary rounded-md">
              {skill.skillCode}
            </span>
          </h1>
        </div>
      </div>

      <div className="p-6 overflow-y-auto flex-1">
        <div className="max-w-5xl mx-auto space-y-6">

          {/* General Information */}
          <div className="grid gap-6 md:grid-cols-3">
            <div className="md:col-span-1 space-y-6">
              <div className="p-5 rounded-2xl bg-card border border-border/50 shadow-sm">
                <h3 className="text-sm font-semibold text-foreground mb-4 flex items-center gap-2">
                  <Bookmark size={16} className="text-primary" />
                  Classification
                </h3>
                <div className="space-y-4">
                  <div>
                    <p className="text-xs text-muted-foreground mb-1">Category</p>
                    <p className="text-sm font-medium text-foreground">{skill.category}</p>
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground mb-1">Subcategory</p>
                    <p className="text-sm font-medium text-foreground">
                      {skill.subcategory || <span className="italic text-muted-foreground/50">None</span>}
                    </p>
                  </div>
                </div>
              </div>
            </div>

            <div className="md:col-span-2">
              <div className="p-5 rounded-2xl bg-card border border-border/50 shadow-sm h-full">
                <h3 className="text-sm font-semibold text-foreground mb-4 flex items-center gap-2">
                  <FileText size={16} className="text-primary" />
                  General Description
                </h3>
                <p className="text-sm text-muted-foreground whitespace-pre-wrap leading-relaxed">
                  {skill.description || "No general description provided for this skill."}
                </p>
              </div>
            </div>
          </div>

          {/* Level Details */}
          <div>
            <h3 className="text-lg font-semibold text-foreground mb-4 flex items-center gap-2">
              <Layers size={20} className="text-primary" />
              Level Requirements
            </h3>

            {skill.levels && skill.levels.length > 0 ? (
              <div className="grid gap-4">
                {skill.levels.map((levelObj) => (
                  <div key={levelObj.id || levelObj.level} className="flex gap-4 p-5 rounded-2xl bg-card border border-border/50 shadow-sm hover:border-primary/30 transition-colors">
                    <div className="shrink-0 flex flex-col items-center">
                      <span className="flex items-center justify-center w-10 h-10 rounded-xl bg-primary/10 text-primary font-bold text-lg">
                        {levelObj.level}
                      </span>
                      <span className="text-[10px] font-medium text-muted-foreground mt-1 uppercase tracking-wider">Level</span>
                    </div>
                    <div className="flex-1 pt-1">
                      <p className="text-sm text-foreground whitespace-pre-wrap leading-relaxed">
                        {levelObj.description || <span className="italic text-muted-foreground">No description available for this level.</span>}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="p-8 rounded-2xl border-2 border-dashed border-border/50 flex flex-col items-center justify-center text-muted-foreground bg-muted/5">
                <Layers size={32} className="opacity-20 mb-3" />
                <p className="text-sm">This skill has no detailed level descriptions configured.</p>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
