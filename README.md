﻿# NEOCP Helper

## Introduction

NEOCP Helper is a plugin for the advanced NINA sequencer that facilitates the programming of sessions for objects that appear on the NEOCP of the Minor Planet Center. When the plugin is properly configured, its operation is very simple. The user selects the NEOs they want to photograph based on the usual parameters of altitude, magnitude, number of observations, etc., and the plugin will automatically program NINA sessions in which, halfway through the session, the NEO will be centered on the sensor and leave a trace of the specified pixel length.

To perform this task, the plugin relies on 4 elements:

- The plugin's configuration options.
- The NEOCP Object List Container, where initial data for the NEO is obtained, and sessions associated with each object are generated and contained.
- The ""Update NEO Session"" instruction that reads ephemerides in real-time when executed, calculates the optimal exposure time, and determines the maximum session duration to keep the asteroid within the sensor range.
- A template for taking photos that the user must adapt to their needs. It should include the ""Update NEO Session"" and ""Take Exposure"" commands and a ""Loop for Time Span"" loop.

## NEOCP Object List Container

The NEOCP Object List Container is the container for NEO sessions and the place from which NEOs are downloaded and selected. Once downloaded, they appear in a table that can be sorted by any of the columns, and a NEO can be selected to generate a session for photographing it.

The generated session will be contained within the container, simplifying the organization of the different sessions.

The structure of the observation session for each specific NEO is defined by the template.

## Template

To observe each object, the plugin expects the user to have previously defined a template. The most basic template contains four instructions:

- **Slew and Center:** locates the necessary sky position to ensure that the NEO trace during the session will be contained within the desired sensor region.
- **Take Exposure:** where the camera binning and maximum exposure time per shot are specified.
- **Loop Time Span:** here we indicate the total duration of the session for each object.
- **Update NEO Session:** This instruction obtains data from NEOCP and updates all parameters used in the aforementioned instructions.

You can download a basic example template for use in NEOCP Helper at [this link](https://github.com/rbarbera/NINA.RBarbera.Plugin.NEOCPHelper/releases/download/static/NEO-basic-session.template.json).

## Operation of the ""Update NEO Session"" instruction.

What characterizes object observation in the NEOCP is the volatility of the presented data. As different observers obtain new data from these objects, their orbital elements are recalculated and position and velocity predictions vary. Even in some cases, objects are removed from the NEOCP list. This variability means that programming of objects done with several hours difference to the observation time becomes invalidated by the latest data.

NEOCP Helper attempts to solve this problem with the ""Update NEO Session"" instruction. This instruction must be introduced in any container with coordinates and target name. For example, in a Deep Sky Observer Container. Its operation is quite straightforward: It uses the Target Name of the container to try to download the ephemeris of that NEO from the Minor Planner Center. If we have configured a prefix for our objects (to facilitate the organization of images), this prefix is removed before making the query.

Once updated ephemerides are obtained, Update NEO Session calculates:

- The maximum exposure time in each take to respect the maximum length (in pixels) we want the NEO to leave on the image. For these calculations, the pixel size of the camera, the focal length of the telescope, and the binning configuration in the ""Take Exposure"" instruction contained in the same container are used.
- The maximum total duration of the session to ensure that the NEO will be within the configured sensor fraction.
- The coordinates of the center of the field containing the entire calculated session.

With this data, the plugin proceeds to modify the template. The container coordinates are updated. As exposure time for each shot, it uses the minimum between the value configured in the template when it is loaded and the one calculated by the plugin for each object. It also searches for the ""Loop Time Span"" in the template and updates it with the minimum of the duration configured in the template or the value calculated for this object.

It is very important to understand that this instruction respects the maximum values of Loop Time Span (session duration) and Exposure (Take Exposure) that exist in the session at the time it is executed. If in the session the duration of the Loop for Time Span is 1 hour, even if the asteroid moves slowly and can be in the field for 2 hours, the session time will remain at 1 hour. If the NEO is very bright and we consider that 30 minutes of exposure will be enough, we must set a value of 30 minutes in the Loop for Time Span section. Similarly, for exposure, the plugin will never exceed the value set in the Take Exposure instruction, as it is considered a maximum value.

## Configuration Parameters

To perform all these calculations and obtain the necessary information, the plugin has several parameters. Let's see them organized by categories:

### NEOCP interface

These are the same parameters used in the NEOCP to filter objects. The meaning and ranges are exactly the same as on that page.

- **V between:** Magnitude range displayed in the list
- **Dec between:** Declination range displayed in the list
- **Score between:** Score range displayed in the list

Regarding the observer's coordinates, to avoid duplications and inconsistencies with the rest of the N.I.N.A. configuration, the data configured in the app is used instead of the observatory code.

### Session Characteristics

- **Max Target Trace:** NEOs move across the sensor and often do it fast. This value is the maximum number of pixels it will move across the sensor in 1 exposure regardless of the binning used.
- **Sensor Area used:** Based on pixel size, focal length, and resolution values, a circle inscribed in that sensor is calculated which corresponds to the value of 100%. This value indicates the area of the sensor that is considered valid. It affects the time the NEO can stay without leaving that area and therefore limits the maximum duration of the session.

### Integration with N.I.N.A.

- **Force Skip on Failure:** It is possible that when programming a NEO, it is listed in the NEOCP but at the time of execution, for whatever reason, it no longer exists on the NEOCP page. This option determines whether the plugin should use the coordinates determined when it was programmed or if it should just skip that NEO.
- **Expand Inserted Templates:** Indicates whether a newly created object from the NEOCP Object List Container should be displayed expanded or collapsed. The default value is collapsed because it speeds up the UI.
- **NEO Target Prefix:** Prefix added to the name of the NEO to finish the name of the generated image files.
- **NEO Container template:** The template that the plugin will use for session creation. The template must be created beforehand.

*Tip:* If you want to photograph an NEO that is moving extremely fast and you cannot obtain 3 good observations because it goes off the sensor very quickly, you can create a session for the NEO and replicate it 3 times. For each of the sessions, N.I.N.A. will recentre the NEO and we can obtain 1 good observation from each of the consecutive sessions.

## Contact

- If you have any ideas or want to report an issue, please contact me in the [Nina discord server](https://discord.gg/rWRbVbw) and tag me: @rbarbera#1806
- If you want to keep in contact with our group of NEO observers, you can reach us on [Asociacion Valenciana de Astronomía](https://astroava.org/neocp-helper/). We have extensive experience in [astrometry studies](https://astroava.org/astrometria/) of minor bodies of our Solar System.