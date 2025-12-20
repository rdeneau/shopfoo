#!/usr/bin/env node

import { readFileSync, writeFileSync } from 'fs';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

// Get the new version from semantic-release environment variable
const newVersion = process.env.npm_package_version || process.argv[2];

if (!newVersion) {
  console.error('Error: No version provided');
  process.exit(1);
}

// Read package.json
const packageJsonPath = join(__dirname, 'package.json');
const packageJson = JSON.parse(readFileSync(packageJsonPath, 'utf-8'));

// Update releaseDate with current date in YYYY-MM-DD format
const today = new Date();
const releaseDate = today.toISOString().split('T')[0];

packageJson.releaseDate = releaseDate;

// Write back to package.json
writeFileSync(packageJsonPath, JSON.stringify(packageJson, null, 2) + '\n', 'utf-8');

console.log(`✓ Updated releaseDate to ${releaseDate} in package.json`);
