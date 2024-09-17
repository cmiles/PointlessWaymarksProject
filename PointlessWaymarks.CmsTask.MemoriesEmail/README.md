## Memories Email Task

This program will create emails with links to content created X years back from Pointless Waymarks CMS Site. The Memories Email Task is a Console app intended to be run as a periodic task in Windows Task Scheduler.

The information below covers how the program works and the details of the required settings file - but first a couple of suggestions:
 - Consider making/using a dedicated email account for this process that is not 'connected' to any of your other email accounts. This program does not share your email account information with other services and should be as secure as the computer/account it is run under, but since it does NOT need to do anything except send emails (it does not read or download email, interact with your calendar, etc.) so it is a 

Notes:
  - A JSON settings file controls how the program works and the settings file must be passed on the command line to the program
  - The settings file holds unsername and password information IN UNENCRYPTED PLAINTEXT FILES!!! For many uses/computers/locations this may make the program unsuitable and too insecure to use. Consider creating/using a dedicated email account for this program that you don't use for anything else and consider whether your storage location for the settings file is secure enough to use this program!
  - The program works by parsing data from a 'live' site - it can work with sites behind Basic Auth but is not setup to work from local only sites.

The settings file is a JSON file - a sample is included with the program.

Required Settings:
  - "siteUrl": "http://example.com"
  - "smtpHost": "smtp.gmail.com" -> This program uses SMTP to send the email...
  - "fromEmailAddress": "someone@test.com"
  - "fromEmailPassword": "your password in plain text" -> As noted above this will be stored unencrypted for anyone who can open the file to read - see the notes above and decide for yourself if this is appropriate.
  - "toAddressList": "someone@test.com; someoneelse@test.com" -> a semicolon seperated list of email addresses to send to, at least one address is required.

Can be omitted to use a default value:
  - "yearsBack": [ 100, 90, 80, 70, 60, 50, 40, 30, 20, 10, 5, 3, 2, 1 ] -> this is an array of integer that defines the years back the program will look for content, all of the years will be combined into a single email. The default is 10, 5, 2, 1.
  - "referenceDate": "2022-10-1" -> This can be used to create an email from a date other than the default of 'yesterday' - mainly useful for testing.
  - "smtpPort": 587 -> Will default to 587
  - "enableSsl": true -> Defaults to true
  - "fromDisplayName": "Pointless Waymarks Emailer" -> Email from display name - defaults to 'Pointless Waymarks Memories'

Optional Settings:
  - "basicAuthUserName": "" -> Leave blank unless the site is using Basic Auth - this option is included in part because Cloudflare workers make putting and Amazon S3 Static Site behind basic auth free and easy.
  - "basicAuthPassword": "" -> As noted above this will be stored unencrypted for anyone who can open the file to read - see the notes above and decide for yourself if this is appropriate.