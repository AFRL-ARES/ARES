---
title: 'ARES OS 2.0: An Orchestration Software Suite for Autonomous Experimentation Systems and Self-Driving Labs'
tags:
 - Automation
 - Autonomy
 - Self-driving lab
 - Self-driving Laboratories
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
   orcid: 0009-0002-1849-5833
   affiliation: 5
 - name: Benji Maruyama
   orcid: 0000-0002-3832-628X
   affiliation: 1

affiliations:
 - name: Air Force Research Laboratory, Foundational Technologies Directorate, United States of America
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
`ARES OS 2.0` (hereinafter `ARES OS`) is an open-source software suite to enable laboratory automation and closed-loop autonomous experimentation. Its function is to orchestrate experimental actions and data handoff between lab equipment, analysis routines, and experimental planning modules through a service-oriented architecture. `ARES OS` is abstracted to apply to general experimental flows common in materials science, chemistry, biology, and related disciplines. The core of `ARES OS` provides central control over all modules, along with the heavy lifting of UI creation, data management, and experimental design tools. `ARES OS` modules communicate with the core software over `protobuf`[@protbuf] and `gRPC`[@grpc], allowing them to be language-agnostic and user-creatable. This allows users to easily implement modules that control experimental hardware, process collected data, or plan experiments to meet their specific research needs. `ARES OS` lowers the barrier to entry for researchers to build their own self-driving labs, allowing them to focus on scientific programming for their use case and reducing the effort and time needed to bring an autonomous experimentation system online. 

# Statement of Need
Research and technology development in the physical sciences has historically been a slow, expensive, and labor-intensive process. To overcome these issues and accelerate the pace of discovery, researchers across a variety of fields have started a revolution in how science is done: autonomous experimentation (AE). AE, also called self-driving labs (SDLs), combines robotic high-throughput experimentation (HTE) techniques with in situ and in-line analysis methods, and artificial intelligence/machine learning (AI/ML) planning routines to autonomously plan, execute, and analyze experiments in pursuit of a user-defined goal [@stach:2021; @abolhasani:2023], with the objective of making scientific research faster, better, and cheaper. This process flow is shown in \autoref{fig:fig_1}. AE systems have been demonstrated to provide faster research progress, lower experimental variability, and a reduced number of experiments to reach a goal compared to traditional manual planning and experimentation [@stach:2021]. Our group published the first autonomous experimentation system, ARES, for materials in 2016, and `ARES OS` has been in development since [@nikolaev:2016].

![A closed loop, research autonomy process flow. Used with permission from @stach:2021. Copyright Elsevier 2021. \label{fig:fig_1}](figure_1.jpg)

Implementing a new SDL traditionally has a high barrier to entry, with software being a major contributor[@lo:2024]. Today there is a growing number of SDL orchestration software offerings, and while some are low-cost and/or open-source, they tend to be specific to a research domain. Many SDLs rely either on expensive offerings from commercial vendors, or are bespoke, researcher-built systems, with long implementation timelines due to the complexity and array of technical disciplines required to successfully develop and integrate all aspects of an SDL [@lo:2024]	(e.g., software architecture, mechatronics, AI/ML, domain-specific scientific knowledge). These factors pose a high barrier to entry and constrain SDL development to well-funded research organizations, slowing the application of SDLs to new research problems. 

From a researcher's standpoint, the largest hurdle in developing an SDL is the integration of separate elements into a functioning autonomous system [@seifrid:2022]. Thanks to the wealth of data analysis, ML, and other scientific libraries available, many researchers have sufficient competence with Python to create the individual modules of an autonomous system but may lack the software engineering expertise to integrate them in a robust and flexible manner. `ARES OS` was developed to address this core issue by providing researchers with a modular framework for coordinating hardware, software, and data management. This framework is combined with an easy-to-use, self-populating UI and companion Python library, `PyAres` [@PyAres], which allows users to rapidly develop, test, and integrate system components.

# State of the Field
Several other open-source SDL orchestration software packages are available, primarily due to the novelty of the field. Notable examples include MadSci[@MADSci], ChemOS2.0[@sim2024], Minerva-OS[@zaki2025], and Ivory OS[@zhang2025a].

One way `ARES OS` differentiates itself from other software packages is its autonomy-first approach to lab orchestration. While capable of operating without them, ARES OS expects closed-loop experimental analysis and planning to be part of every experimental campaign, placing these features directly in front of the user. This is also reflected in our PyAres companion library, with analyzer and planner service modules distinct from devices, allowing researchers to supply their own. Of the four other packages highlighted, ChemOS2.0 and IvoryOS have integrated support for closed-loop experimental planning, and MinervaOS lists this feature in beta. As far as could be discerned, these packages also only support semi-baked in optimization algorithms and do not have a clear path for users to supply their own experimental planning logic.

This degree of granular modularity is another way `ARES OS` differentiates itself: breaking out modules for device interaction (software or hardware), data analysis, and experimental planning. IvoryOS, for example, bakes analysis into the hardware device driver, where the device driver returns processed data with one method call. While it is possible to use this approach within `ARES OS`, the default approach is to split the data collection and data analysis into different processes, allowing both detailed raw data and processed results to be automatically catalogued in the experiment database entry. 

Additionally, `ARES OS` offers the most straightforward installation process of any of the availible orchestrators. Simply downloading and running the ARES Launcher will handle installation, updates, database management, version management, and PyAres service management in a friendly, graphical interface. The other software packages highlighted here include detailed installation instructions but rely on software workflows that an experimental scientist may not be familiar with. The preferred installation method for MADSci is Docker (though installation though PyPi is also an option), ChemOS2.0 utilizes nixos, MinervaOS follows a manual python package-style installation with nested repositories, and IvoryOS installs via PyPi. 

# Software Design
`ARES OS` uses a service-oriented architecture with a C# and ASP.NET core, written to follow SOLID principles for understandability, flexibility, and maintainability [@martin2003agile]. The core application handles the backend logic necessary for automation and autonomy, such as experimental routines and database interactions (ARES OS supports SQL Server, SQLite, and Postgres) and provides frameworks for interacting with system modules, such as custom GUIs, laboratory hardware, experimental planners, and data analyzers. 

Communication between the core and system module services is facilitated by Google's protobuf and gRPC. The use of protobuf allows for easy data transmission over the network, facilitating the use of both local and remote experimental or computing resources. Protobuf also allows ARES OS to be language-agnostic, enabling the creation or re-use of modules written in any supported language (e.g., C#, Python, JavaScript, R). 

By default, `ARES OS` includes a Blazor UI, designed as an intuitive hub for customizing and using an AE system, allowing for both centralized computer control of experimental hardware and the execution of user-defined campaigns for automated or autonomous experimentation. The `PyAres` library is available via PyPI and provides an easy-to-use interface to create and configure `ARES OS`-compatible devices, planners, and analyzers with only a few lines of Python code [@PyAres]. For ease of use, we have also created an `ARES OS` launcher application, which streamlines the installation and configuration of `ARES OS` and the necessary databases and certificates [@ARES-Launcher]. The `ARES OS` launcher also supports installation from specific forks of `ARES OS` to enable users to develop modified versions that fit their specific use cases.

# Research Impact Statement
`ARES OS` was designed primarily to be used by experimental researchers in the physical sciences for the implementation of AE/SDL systems. Additionally, `ARES OS` is suitable for use by students in a classroom setting to study ML and AE principles. As part of its development, `ARES OS` has been used in experimental systems to study a variety of materials science problems such as carbon nanotube synthesis [@waelder:2024; @bulmer:2023] and fused deposition modeling 3D printing [@deneault:2021]. ARES OS will also be used in a new curriculum under development by the University at Buffalo’s Department of Materials Design and Innovation. 

# Availability
The `ARES OS` Core source code is available from the public GitHub repository (<https://github.com/AFRL-ARES/ARES/releases>). The `ARES OS` launcher source code is available from the public GitHub repository (<https://github.com/AFRL-ARES/ARES-Launcher/releases>) with downloadable binaries for Linux, Windows and macOS. The `PyAres` companion library is available on PyPI (<https://pypi.org/project/PyAres>) for installation with `pip` or from the public GitHub repository (<https://github.com/AFRL-ARES/PyAres/releases>).

# AI Usage Disclosure
Multiple versions of Google Gemini and OpenAI ChatGPT were used during the development of `ARES OS` to generate templates, test new concepts, review code, and write documentation. All AI output was reviewed, modified and validated by human team members. No generative AI was used in the preparation of this manuscript.

# Acknowledgements
The authors gratefully recognize funding from the Air Force Office of Scientific Research under LRIR 25COR019, R. Doug Riecken, PO.

# References

