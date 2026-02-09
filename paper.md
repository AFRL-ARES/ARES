---
title: 'ARES OS 2.0: An Orchestration Software Suite for Autonomous Experimentation Systems and Self-Driving Labs'
tags:
 - Automation
 - Autonomoy
 - Self-driving lab
 - Self-driving Laboritories
 - Autonomous Experimentation
 
authors:
 - name: Arthur W. N. Sloan
   orcid: 0000-0002-7066-1678
   affiliation: "1, 2"
 - name: Robert W. Waelder
   orcid: 0000-0001-6958-2932
   affiliation: "1, 3"
 - name: Morgen L. Smith
   orcid: 0009-0003-1849-7051
   affiliation: "1, 3, 4"
 - name: Nicholas Kleiner
   affiliation: 5
 - name: Arnas Babeckis
   affiliation: 5
 - name: Jason Wheeler
   affiliation: 5
 - name: Daylond Hooper
   affiliation: 5
 - name: Benji Maruyajma
   orcid: 0000-0002-3832-628X
   affiliation: 1

affiliations:
 - name: Air Force Research Laboratory, Materials & Manufacturing Directorate, United States of America
   index: 1
 - name: The National Research Council, United States of America
   index: 2
 - name: AV Inc., United States of America
   index: 3
 - name: Kansas State University, Tim Taylor Department of Chemical Engineering, United States of America
   index: 4
 - name: DCS Corp., United States of America
   index: 5
date: 30 January 2026
bibliography: paper.bib

---

# Summary
`ARES OS 2.0` (hereinafter `ARES OS`) is an open-source software suite to enable laboratory automation and closed-loop autonomous experimentation. Its function is to orchestrate experimental actions and data handoff between lab equipment, analysis routines, and experimental planning modules through a service-oriented architecture. `ARES OS` is abstracted to apply to general experimental flows common in materials science, chemistry, and biology and related disciplines. The core of `ARES OS` provides central control over all modules, along with the heavy lifting of UI creation, data management, and experimental design tools. `ARES OS` modules communicate with the core software over `protobuf` and `gRPC`, allowing them to be language-agnostic and user-creatable. This allows users to easily implement modules that control experimental hardware, process collected data , or plan experiments to meet their specific research needs. `ARES OS` lowers the barrier to entry for researchers to build their own self-driving labs, allowing them to focus on scientific programming for their use case and reducing the effort and time needed to bring an autonomous experimentation system online. 

# Statement of Need
Research and technology development in the physical sciences has historically been a slow, expensive, and labor-intensive process. To overcome these issues and accelerate the pace of discovery, researchers across a variety of fields have started a revolution in how science is done: Autonomous Experimentation (AE). AE, also called self-driving labs (SDLs), combines robotic high throughput experimentation (HTE) techniques with in situ and in-line analysis methods, and artificial intelligence/machine learning (AI/ML) planning routines to autonomously plan, execute, and analyze experiments in pursuit of a user defined goal [@stach:2021; @abolhasani:2023], with the objecting of making scientific research faster, better, and cheaper. This process flow is shown in \autoref{fig:fig_1}. AE systems have been demonstrated to provide faster research progress, lower experimental variability, and a reduced number of experiments to reach a goal compared to traditional manual planning and experimentation [@stach:2021]. Our group published the first autonomous experimentation system, ARES, for materials in 2016, and `ARES OS` has been in development since [@nikolaev:2016].

![A closed loop, research autonomy process flow. Used with permission from @stach:2021. Copyright Elsevier 2021. \label{fig:fig_1}](figure_1.jpg)

Implementing a new SDL traditionally has a high barrier to entry, with software being a major contributor[@lo:2024]. Today there is a growing number of SDL orchestration software offerings, and while some are low cost and/or open-source, they tend to be specific to a research domain. Many SDLs rely either on expensive offerings from commercial vendors, or are bespoke, researcher-built systems, with long implementation timelines due to the complexity and array of technical disciplines required to successfully develop and integrate all aspects of an SDL [@lo:2024]	(e.g., software architecture, mechatronics, AI/ML, domain specific scientific knowledge). These factors pose a high barrier to entry and constrain SDL development to well-funded research organizations, slowing the application of SDLs to new research problems. 

From a researcher standpoint, the largest hurdle in developing an SDL is the integration of separate elements into a functioning autonomous system [@seifrid:2022]. Thanks to the wealth of data analysis, ML, and other scientific libraries available, many researchers have sufficient competence with Python to create the individual modules of autonomous system but may lack the software engineering expertise to integrate them in a robust and flexible manner. `ARES OS` was developed to address this core issue by providing researchers with a modular framework for coordinating hardware, software, and data management. This framework is combined with an easy-to-use, self-populating UI and companion Python library, `PyAres` [@PyAres], which allows users to rapidly develop, test, and integrate system components.

# State of the Field
Several other open-source SDL orchestration software packages are available. Notable examples include MadSci[@MADSci], ChemOS2.0[@sim2024], and Minerva-OS[@zaki2025]. Compared to these alternatives, `ARES OS` differentiates itself primarily through the researcher-first user experience, which places an emphasis on low- or no-code operation of core features. All interactions with the core functionality of `ARES OS` can be accomplished within the GUI. This includes installation, analyzer/planner module configuration, hardware control, building and executing experimental campaigns, and data export. `ARES OS` has also been successfully abstracted to several different domains of scientific research including additive manufacturing, wet chemistry, and chemical vapor deposition, while other software offerings may to be more specialized to a single domain. 

# Software Desgin
`ARES OS` uses a service-oriented architecture with a C# and ASP .NET core, written to follow SOLID principles for understandability, flexibility, and maintainability. The core application handles the backend logic necessary for automation and autonomy, such as experimental routines, database interactions (ARES OS supports SQL Server, SQLite, and Postgres), and provides frameworks for interacting with system modules, such as custom GUIs, laboratory hardware, experimental planners, and data analyzers. 

Communication between the core and system module services is facilitated by Google's protobuf and gRPC. The use of protobuf allows for easy data transmission over the network, facilitating the use of both local and remote experimental or computing resources. Protobuf also allows ARES OS to be language-agnostic, enabling the creation or re-use of modules written in any supported language (e.g., C#, Python, Javascript, R, etc.). 

By default, `ARES OS` includes a Blazor UI, designed as an intuitive hub for customizing and using an AE system, allowing for both centralized computer control of experimental hardware and the execution of user-defined campaigns for automated or autonomous experimentation. The `PyAres` library is available via PyPi and provides an easy-to-use interface to create and configure `ARES OS` compatible devices, planners, and analyzers with only a few lines of Python code [@PyAres]. For ease of use we have also created an `ARES OS` launcher application, which streamlines the installation and configuration of `ARES OS` and the necessary databases and certificates [@ARES-Launcher]. The `ARES OS` launcher also supports installation from specific forks of `ARES OS` to enable users to develop modified versions that fit their specific use cases.

# Research Impact Statement
`ARES OS` was designed primarily to be used by experimental researchers in the physical sciences for the implementation of AE/SDL systems. Additionally, `ARES OS` is suitable for use by students for use in a classroom setting to study ML and AE principles. As part of its development, `ARES OS` has been used in experimental systems to study a variety of materials science problems such as carbon nanotube synthesis [@waelder:2024; @bulmer:2023] and fused deposition modeling 3D printing [@deneault:2021]. ARES OS will also be used in new curriculum under development by the University of Buffalo’s department of Materials Design and Innovation. 

# Availability
The `ARES OS` Core source code is available from the public GitHub repository (<https://github.com/AFRL-ARES/ARES/releases>). The `ARES OS` launcher source code is available from the public GitHub repository (<https://github.com/AFRL-ARES/ARES-Launcher/releases>) with downloadable binaries for Linux, Windows and MacOS. The `PyAres` companion library is available on PyPi (<https://pypi.org/project/PyAres>) for installation with `pip` or from the public GitHub repository (<https://github.com/AFRL-ARES/PyAres/releases>).

# AI Usage Disclosure
Multiple versions of Google Gemini and OpenAI ChatGPT were used during the devleopment of `ARES OS` to generate templates, test new concepts, review code, and write documentation. All AI output was reviewed, modified and validated by human team members. No generative AI was used in the preparation of this manuscript.

# Acknowledgements
The authors gratefully recognize funding from the Air Force Office of Scientific Research under LRIR 25COR019, R. Doug Riecken, PO.

# References

