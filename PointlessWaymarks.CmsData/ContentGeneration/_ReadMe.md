There should be a generator class for every Content Type which is the central way that you validate, save and generate HTML for a type.

In general the Generator classes should be used because they already combine the various steps that are needed ->
    For example when you call SaveAndGenerateHtml the generator class will:
     - Validate the data
     - Save it to the database with the correct methods to also create Data Notifications
     - Write the JSON content to the ContentData Directory and log the write to the File Log
     - Write historic entries to a JSON History File and log the write to the File Log
     - Generate the HTML and log the write to the File Log

In most cases those actions are all wrapped into other classes and methods - but the list above (plus some special steps for content like Lines that need a GPX file generated - or Photos that have images to deal with) should be enough to convice you that the Generator abstraction is worthwhile.