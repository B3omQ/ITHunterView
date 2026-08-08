const fs = require('fs');
const enPath = 'messages/en.json';
const viPath = 'messages/vi.json';

const en = JSON.parse(fs.readFileSync(enPath, 'utf8'));
const vi = JSON.parse(fs.readFileSync(viPath, 'utf8'));

if (!en['CandidateResumes']) en['CandidateResumes'] = {};
if (!vi['CandidateResumes']) vi['CandidateResumes'] = {};

en['CandidateResumes']['cancel'] = "Cancel";
vi['CandidateResumes']['cancel'] = "Hủy";

fs.writeFileSync(enPath, JSON.stringify(en, null, 2));
fs.writeFileSync(viPath, JSON.stringify(vi, null, 2));

console.log('Added cancel key to CandidateResumes successfully!');
