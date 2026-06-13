---
title: Getting Started with AzureWebsite
date: 2026-06-13
author: Admin
category: Tutorial
summary: Learn how to set up and run the AzureWebsite blog system.
tags: [getting-started, tutorial, setup]
draft: false
---

# Getting Started with AzureWebsite

Welcome to the AzureWebsite blog! This post will walk you through the basics of the blog system.

## What is AzureWebsite?

AzureWebsite is a modern ASP.NET Core Razor Pages application that combines a traditional website with a markdown-driven blog system. It's designed to be simple, fast, and easy to extend.

## Key Features

- **Razor Pages**: Pure Razor Pages architecture with no controllers
- **Markdown Blog**: Write blog posts as markdown files with YAML frontmatter
- **Caching**: Intelligent caching with IMemoryCache and OutputCache middleware
- **Responsive Design**: Clean, mobile-friendly layout
- **Health Checks**: Built-in `/healthcheck` endpoint for monitoring

## Writing Your First Blog Post

To create a new blog post, simply add a `.md` file to the `Data/blog/` directory:

```markdown
---
title: My First Post
date: 2026-06-14
author: Your Name
category: General
summary: A brief description of your post.
tags: [tag1, tag2]
draft: false
---

Your markdown content goes here...
```

## Running Locally

Use the following command to run the site locally:

```bash
dotnet watch run --project src/AzureWebsite/AzureWebsite.csproj
```

## Next Steps

- Explore the [architecture documentation](/docs/architecture)
- Check out the [API reference](/docs/api)
- Contribute to the [GitHub repository](https://github.com/your-org/azurewebsite)

Happy blogging!
