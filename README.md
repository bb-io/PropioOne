# Blackbird.io Propio ONE

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

Documentation coming soon.

### Order

- **Create order**: Creates a new order.
- **Get order**: Retrieves details of a specific order.
- **Cancel order**: Cancels an existing order.
- **Download translated target file**: Downloads the translated target file for a given order.
- **Download all translated files**: Downloads the translated target files for a given order.

### AI / Machine Translation

- **Translate text**: Translates the provided text from the source language to the target language using MT.
- **Translate**: Translates files from a source language to a target language using MT. Supported files: blackbird strategy (XLIFF, HTML), propio strategy (DOCX, XLSX, PPTX, HTML, XML, JSON).
- **Edit text**: Post-edits already translated text.
- **Edit**: Edits a translation using Propio APE. Assumes translated content was produced earlier.
- **Review text**: Reviews translation quality using Basic quality estimation for source and target text.
- **Review**: Reviews a translated file using Basic QE. Supports blackbird (segment-based) and propio (file-to-file) strategies.

### Webhooks

- **On order created**: Triggers when a new order is created.


## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
