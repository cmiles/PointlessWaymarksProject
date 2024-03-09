The most durable way to address Content in the Pointless Waymarks CMS is thru 'bracket codes' which are translated by the code into the appropriate HTML.

{{photo 29b32866-355e-4412-bce4-92822956fa1c; 2024 March ReadMe Example}} is an example - this format is not great for human writability, but it is decent enough for human readability, has broad support in the GUI (for example you can drag content out of a list into a Post Body and bracket codes will be inserted) and allows reference to content regardless of whether its slug, folder, title or other details change.

For each Bracket Code there should be a processor file in this directory - beware that the code bracket codes must be manually 'wired' into the content generation (reflection is NOT used to locate all the bracket codes).