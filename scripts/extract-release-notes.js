const fs = require('fs');
const path = require('path');

const changelogPath = path.join(process.cwd(), 'CHANGELOG.md');

try {
  const changelogContent = fs.readFileSync(changelogPath, 'utf8');
  const lines = changelogContent.split('\n');

  let latestVersionHeaderIndex = -1;
  for (let i = 0; i < lines.length; i++) {
    if (lines[i].startsWith('## ')) {
      latestVersionHeaderIndex = i;
      break;
    }
  }

  if (latestVersionHeaderIndex === -1) {
    console.error('Error: No version header (e.g., "## 1.2.3") found in CHANGELOG.md');
    process.exit(1);
  }

  let releaseNotes = [];
  for (let i = latestVersionHeaderIndex + 1; i < lines.length; i++) {
    if (lines[i].startsWith('## ')) {
      break;
    }
    releaseNotes.push(lines[i]);
  }

  while (releaseNotes.length > 0 && releaseNotes[0].trim() === '') {
    releaseNotes.shift();
  }
  while (releaseNotes.length > 0 && releaseNotes[releaseNotes.length - 1].trim() === '') {
    releaseNotes.pop();
  }

  console.log(releaseNotes.join('\n'));

} catch (error) {
  if (error.code === 'ENOENT') {
    console.error(`Error: CHANGELOG.md not found at ${changelogPath}`);
  } else {
    console.error('Failed to extract release notes:', error);
  }
  process.exit(1);
}