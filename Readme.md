# Azure Function: Extract Archive in Blob Storage

This Azure Function triggers on .zip archives uploaded to Blob Storage.

## Settings
- `AzureWebJobsStorage`: Azure Storage connection string
- `InputContainerName`: Azure Storage container to listen on for input
- `OutputContainerName`: Azure Storage container to output extracted files to
- `InputPrefix`: Prefix to listen to, without leading or trailing slashes
- `OutputPrefix`: Prefix prepended to extracted files, without leading or trailing slashes

## Email Notification
After extraction an email is sent using SendGrid. To enable, create an account and fill out the following app settings.  
- `SendGridApiKey`: SendGrid Api Key
- `EmailTo`: a **space-separated** list of email addresses to send notifications to
- `EmailFrom`: email address to display as sender