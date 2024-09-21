# Virtual Customer Success Manager (VCSM) - README

## Table of Contents
- [Overview](#overview)
- [Features](#features)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
  - [Clone the repository](#1-clone-the-repository)
  - [Set up User Secrets](#2-set-up-user-secrets)
  - [Install Required Packages](#3-install-required-packages)
- [Usage](#usage)
  - [Run the Project](#1-run-the-project)
  - [Pick an Option](#2-pick-an-option)
- [Project Structure](#project-structure)
- [License](#license)
- [Keywords](#Keywords)
- [Key Topics and Challenges addressed by this project](#Key-Topics-and-Challenges-addressed-by-this-project)
- [Problem or Opportunity Statement](#Problem-or-Opportunity-Statement)
- [Who is this for](#Who-is-this-for)

## Overview
The **Virtual Customer Success Manager (VCSM)** is a C# .NET project that leverages **Semantic Kernel** and **Azure OpenAI** to deliver personalized onboarding, proactive support, and tailored recommendations for Microsoft products. The application dynamically responds to user queries using AI-powered agents and plugins.

[VCSM_Slide Deck](https://github.com/user-attachments/files/17084746/VCSM_Using_SemanticKernel.pdf)

[VCSM_Video](https://youtu.be/5IvVFJR6l1E)

[Hackathon24 Link](https://hackbox.microsoft.com/hackathons/hackathon2024/project/62484)

![Virtual Customer Success Manager](https://github.com/user-attachments/assets/73f1fd77-0260-4fa9-8b8e-e3e441256e59)


## Features
1. **Personalized Onboarding**: Provides step-by-step guidance for setting up and using Microsoft products, using Semantic Kernel Plugins.
2. **Proactive Support**: Implements a multi-agent system to proactively address user issues, ensuring system stability and optimal performance.
3. **Tailored Recommendations**: Offers personalized suggestions and tips based on user behavior and preferences, retrieved via Semantic Kernel's Vector Store.

## Prerequisites
- **.NET SDK** (version 8.0 or later)
- **Azure OpenAI** credentials

## Installation
1. **Clone the repository**
    ```bash
    git clone <repository-url>
    cd <repository-folder>
    ```

2. **Set up User Secrets**
    The project uses **Azure OpenAI** for processing natural language queries. To configure this securely, use `dotnet user-secrets` to store the API key and related configuration.

    ```bash
    dotnet user-secrets set "AzureOpenAI:DeploymentName" gpt-35-turbo-16k
    dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<deployment-name>.openai.azure.com/"
    dotnet user-secrets set "AzureOpenAI:ApiKey" <insert-key>
    ```

3. **Install Required Packages**
    Install the necessary NuGet packages for the project.

    ```bash
    dotnet add package Microsoft.SemanticKernel # version 1.18.2-alpha
    dotnet add package Microsoft.SemanticKernel.Agents # version 1.18.2-alpha
    dotnet add package Microsoft.SemanticKernel.ChatCompletion
    dotnet add package Xunit
    ```

## Usage

1. **Run the Project**
    Execute the `Program.cs` file, and follow the prompts to select a desired feature.
    
    ```bash
    dotnet run
    ```

2. **Pick an Option**
    When running the project, you will be prompted to select from the available functionalities:

    ```text
    Please choose an option (type 'end' to exit):
    1. Personalized Onboarding using Semantic Kernel Plugins
    2. Proactive Support using Multi-Agents
    3. Tailored Recommendations using Vector Store
    Enter input (1, 2, 3, end to exit):
    ```

    - **Option 1**: Personalized onboarding walkthrough using Semantic Kernel Plugins.
    - **Option 2**: Proactive support with issue resolution powered by multi-agent coordination.
    - **Option 3**: Tailored recommendations based on user data stored in the Semantic Kernel Vector Store.

## Project Structure
- **InternalUtilities**: Contains helper utilities for resource handling and configuration.
- **Resources**: Contains documents such as Teams onboarding guides and recommendations.
- **_vcsm**: Core VCSM logic split into distinct features like onboarding, support, and recommendations.

## License
This project is licensed under the MIT License.

## Keywords
AI, virtual assistant, customer success, Microsoft products, onboarding, usage tips, feature recommendations, customer support, personalized advice, business value

## Key Topics and Challenges addressed by this project
- Customer Experience
- Operational Excellence
- AI Transformation

## Problem or Opportunity Statement
Customers struggle to fully use Microsoft products, leading to missed opportunities. An AI assistant offers proactive, personalized support to enhance usage and drive long-term success.

## Who is this for
- Microsoft Product Users
- IT Support Teams
- Customer Success Managers
- Business Decision Makers
- Technology Enthusiasts
