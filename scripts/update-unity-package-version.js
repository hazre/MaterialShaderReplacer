const fs = require('fs');
const path = require('path');

const newVersion = process.env.NEW_VERSION;
const unityPackageJsonPath = process.env.UNITY_PKG_PATH;

if (!newVersion) {
  console.error('Error: NEW_VERSION environment variable is not set.');
  process.exit(1);
}

if (!unityPackageJsonPath) {
  console.error('Error: UNITY_PKG_PATH environment variable is not set.');
  process.exit(1);
}

const resolvedPath = path.resolve(unityPackageJsonPath);

try {
  const pkg = JSON.parse(fs.readFileSync(resolvedPath, 'utf8'));
  pkg.version = newVersion;
  fs.writeFileSync(resolvedPath, JSON.stringify(pkg, null, 2) + '\n');
  console.log(`Updated ${resolvedPath} to version ${newVersion}`);
} catch (error) {
  if (error.code === 'ENOENT') {
    console.error(`Error: Unity package.json not found at ${resolvedPath}`);
  } else {
    console.error(`Failed to update Unity package.json at ${resolvedPath}:`, error);
  }
  process.exit(1);
}