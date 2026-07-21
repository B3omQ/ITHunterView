'use client';

import React, { useEffect, useState } from 'react';
import { PageLoader } from '@/components/shared/PageLoader';
import { EmptyState } from '@/components/shared/EmptyState';
import { recruiterService } from '@/services/recruiter.service';
import { useParams } from 'next/navigation';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { MapPin, Phone, Mail, Link as LinkIcon, Briefcase, GraduationCap, Award, FileText } from 'lucide-react';
import { Badge } from '@/components/ui/badge';

export default function CandidatePublicProfilePage() {
  const params = useParams();
  const id = params?.id as string;
  const [profile, setProfile] = useState<any>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchProfile = async () => {
      setIsLoading(true);
      const res = await recruiterService.getCandidateProfile(id);
      if (res.success && res.data?.data) {
        setProfile(res.data.data);
      } else {
        setError(res.message || 'Could not load candidate profile');
      }
      setIsLoading(false);
    };

    if (id) {
      fetchProfile();
    }
  }, [id]);

  if (isLoading) {
    return (
      <div className="w-full pb-8 space-y-8">
        <PageLoader message="Loading candidate profile..." />
      </div>
    );
  }

  if (error || !profile) {
    return (
      <div className="w-full pb-8 space-y-8">
        <EmptyState
          title="Profile Not Found"
          description={error || "This profile may not exist or is not public."}
        />
      </div>
    );
  }

  const { personalInfo, skills, experiences, educations, certifications } = profile;
  const hasLinks = personalInfo.githubUrl || personalInfo.linkedInUrl || personalInfo.portfolioUrl;

  return (
    <div className="w-full pb-16 space-y-6 max-w-5xl mx-auto">
      <h1 className="text-2xl font-bold tracking-tight mb-6">Candidate Profile</h1>

      <div className="grid grid-cols-1 lg:grid-cols-[350px_1fr] gap-6 items-start">
        {/* Left Column: Personal Info */}
        <div className="space-y-6">
          <Card className="border-slate-200 shadow-sm overflow-hidden">
            <div className="h-24 bg-gradient-to-r from-slate-900 to-slate-800" />
            <CardContent className="pt-0 relative px-6 pb-6">
              <div className="flex flex-col items-center">
                <div className="h-24 w-24 rounded-full border-4 border-white bg-slate-100 overflow-hidden -mt-12 mb-4 flex items-center justify-center">
                  {personalInfo.avatarUrl ? (
                    <img src={personalInfo.avatarUrl} alt={personalInfo.firstName} className="h-full w-full object-cover" />
                  ) : (
                    <span className="text-3xl font-bold text-slate-400">
                      {(personalInfo.firstName?.[0] || '')}{(personalInfo.lastName?.[0] || '')}
                    </span>
                  )}
                </div>
                <h2 className="text-xl font-bold text-slate-900 text-center">
                  {personalInfo.firstName} {personalInfo.lastName}
                </h2>
                <div className="flex items-center gap-1.5 mt-2 text-slate-500 text-sm">
                  <MapPin className="w-4 h-4" />
                  <span>{personalInfo.location || 'Location not specified'}</span>
                </div>
              </div>

              <div className="mt-6 space-y-4 pt-6 border-t border-slate-100">
                <div className="flex items-center gap-3 text-sm text-slate-600">
                  <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-slate-50 text-slate-500">
                    <Mail className="h-4 w-4" />
                  </div>
                  <span className="truncate">{personalInfo.email || 'No email provided'}</span>
                </div>
                <div className="flex items-center gap-3 text-sm text-slate-600">
                  <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-slate-50 text-slate-500">
                    <Phone className="h-4 w-4" />
                  </div>
                  <span>{personalInfo.phone || 'No phone provided'}</span>
                </div>
              </div>
            </CardContent>
          </Card>

          {hasLinks && (
            <Card className="border-slate-200 shadow-sm">
              <CardHeader className="pb-3 border-b border-slate-100">
                <CardTitle className="text-base font-bold flex items-center gap-2">
                  <LinkIcon className="h-5 w-5 text-indigo-500" />
                  Social Links
                </CardTitle>
              </CardHeader>
              <CardContent className="pt-4 space-y-3">
                {personalInfo.githubUrl && (
                  <a href={personalInfo.githubUrl} target="_blank" rel="noreferrer" className="flex items-center gap-2 text-sm text-blue-600 hover:underline">
                    GitHub
                  </a>
                )}
                {personalInfo.linkedInUrl && (
                  <a href={personalInfo.linkedInUrl} target="_blank" rel="noreferrer" className="flex items-center gap-2 text-sm text-blue-600 hover:underline">
                    LinkedIn
                  </a>
                )}
                {personalInfo.portfolioUrl && (
                  <a href={personalInfo.portfolioUrl} target="_blank" rel="noreferrer" className="flex items-center gap-2 text-sm text-blue-600 hover:underline">
                    Portfolio
                  </a>
                )}
              </CardContent>
            </Card>
          )}

          <Card className="border-slate-200 shadow-sm">
            <CardHeader className="pb-3 border-b border-slate-100">
              <CardTitle className="text-base font-bold flex items-center gap-2">
                <FileText className="h-5 w-5 text-indigo-500" />
                Skills
              </CardTitle>
            </CardHeader>
            <CardContent className="pt-4">
              {skills && skills.length > 0 ? (
                <div className="flex flex-wrap gap-2">
                  {skills.map((skill: any) => (
                    <Badge key={skill.id} variant="secondary" className="bg-slate-100 text-slate-700 hover:bg-slate-200 font-medium">
                      {skill.name || skill.skillName}
                    </Badge>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-slate-500">No skills added yet.</p>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Right Column: About, Experience, Education */}
        <div className="space-y-6">
          {personalInfo.aboutMe && (
            <Card className="border-slate-200 shadow-sm">
              <CardHeader className="pb-3 border-b border-slate-100">
                <CardTitle className="text-base font-bold">About Me</CardTitle>
              </CardHeader>
              <CardContent className="pt-4">
                <p className="text-sm text-slate-700 whitespace-pre-wrap leading-relaxed">
                  {personalInfo.aboutMe}
                </p>
              </CardContent>
            </Card>
          )}

          <Card className="border-slate-200 shadow-sm">
            <CardHeader className="pb-3 border-b border-slate-100">
              <CardTitle className="text-base font-bold flex items-center gap-2">
                <Briefcase className="h-5 w-5 text-indigo-500" />
                Work Experience
              </CardTitle>
            </CardHeader>
            <CardContent className="pt-6">
              {experiences && experiences.length > 0 ? (
                <div className="space-y-6 relative before:absolute before:inset-0 before:ml-5 before:-translate-x-px md:before:mx-auto md:before:translate-x-0 before:h-full before:w-0.5 before:bg-gradient-to-b before:from-transparent before:via-slate-200 before:to-transparent">
                  {experiences.map((exp: any, index: number) => (
                    <div key={exp.id || index} className="relative flex items-center justify-between md:justify-normal md:odd:flex-row-reverse group is-active">
                      <div className="flex items-center justify-center w-10 h-10 rounded-full border border-white bg-indigo-100 text-indigo-600 shadow shrink-0 md:order-1 md:group-odd:-translate-x-1/2 md:group-even:translate-x-1/2 z-10">
                        <Briefcase className="w-4 h-4" />
                      </div>
                      <div className="w-[calc(100%-4rem)] md:w-[calc(50%-2.5rem)] p-4 rounded-xl border border-slate-100 bg-white shadow-sm">
                        <div className="flex items-center justify-between mb-1">
                          <h4 className="font-bold text-slate-900">{exp.position}</h4>
                        </div>
                        <div className="text-sm font-medium text-slate-600 mb-2">{exp.companyName}</div>
                        <div className="text-xs text-slate-400 mb-3">
                          {new Date(exp.startDate).toLocaleDateString()} - {exp.isCurrentJob ? 'Present' : new Date(exp.endDate).toLocaleDateString()}
                        </div>
                        <p className="text-sm text-slate-500 whitespace-pre-wrap">{exp.description}</p>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-slate-500 text-center py-4">No experience added yet.</p>
              )}
            </CardContent>
          </Card>

          <Card className="border-slate-200 shadow-sm">
            <CardHeader className="pb-3 border-b border-slate-100">
              <CardTitle className="text-base font-bold flex items-center gap-2">
                <GraduationCap className="h-5 w-5 text-indigo-500" />
                Education
              </CardTitle>
            </CardHeader>
            <CardContent className="pt-6">
              {educations && educations.length > 0 ? (
                <div className="space-y-4">
                  {educations.map((edu: any, index: number) => (
                    <div key={edu.id || index} className="flex gap-4 p-4 rounded-xl border border-slate-100 bg-slate-50/50">
                      <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-indigo-100 text-indigo-600">
                        <GraduationCap className="h-6 w-6" />
                      </div>
                      <div>
                        <h4 className="font-bold text-slate-900">{edu.schoolName}</h4>
                        <div className="text-sm text-slate-600 mt-1">{edu.degree} - {edu.fieldOfStudy}</div>
                        <div className="text-xs text-slate-400 mt-2">
                          {new Date(edu.startDate).toLocaleDateString()} - {edu.isCurrent ? 'Present' : new Date(edu.endDate).toLocaleDateString()}
                        </div>
                        {edu.description && (
                          <p className="text-sm text-slate-500 mt-3 whitespace-pre-wrap">{edu.description}</p>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-slate-500 text-center py-4">No education added yet.</p>
              )}
            </CardContent>
          </Card>

          {certifications && certifications.length > 0 && (
            <Card className="border-slate-200 shadow-sm">
              <CardHeader className="pb-3 border-b border-slate-100">
                <CardTitle className="text-base font-bold flex items-center gap-2">
                  <Award className="h-5 w-5 text-indigo-500" />
                  Certifications
                </CardTitle>
              </CardHeader>
              <CardContent className="pt-6">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {certifications.map((cert: any, index: number) => (
                    <div key={cert.id || index} className="flex gap-3 p-4 rounded-xl border border-slate-100 bg-white shadow-sm hover:border-indigo-100 transition-colors">
                      <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-amber-100 text-amber-600">
                        <Award className="h-5 w-5" />
                      </div>
                      <div className="min-w-0">
                        <h4 className="font-bold text-slate-900 truncate">{cert.name}</h4>
                        <div className="text-sm text-slate-500 truncate">{cert.issuingOrganization}</div>
                        <div className="text-xs text-slate-400 mt-1">
                          Issued: {new Date(cert.issueDate).toLocaleDateString()}
                        </div>
                        {cert.credentialUrl && (
                          <a href={cert.credentialUrl} target="_blank" rel="noreferrer" className="text-xs text-indigo-600 hover:underline mt-1 block">
                            View Credential
                          </a>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
