# Pointless Waymarks Publish Readme Helper

GitHub has made README.md a bit of a requirement because it so easily allows good presentation of information about a project - and this works on a 'sub-project' level as well...

But if you want to take advantage of GitHub's auto-display of README.md and have your Read Me for every referenced project included in published output you quickly run into collisions with the README.md files and need to find a work around.

Many solutions are possible but for the Pointless Waymarks projects I decided to write a small program that would copy any README.md files found under top-level subdirectories to README_[folder name minus the first segment].md. This allows you to still use/maintain a README.md file and get the benefits on GitHub - but also (with the right project settings to never copy README.md and copy if newer README_[folder name minus the first segment].md) allows the Read Me's for all referenced projects to be included in the output.