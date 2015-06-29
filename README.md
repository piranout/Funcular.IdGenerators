# Funcular.IdGenerators

A cross-process thread-safe C# utility to create ascending (but non-sequential), human speakable, case-insensitive, pseudo-random identifiers in Base36.

* Guid: `{7331d71b-d1f1-443b-97f6-f24eeb207828}`
* Base36 [16]: `040VZ3C6SL3BZ2RW` or `040V-Z3C6-SL3B-Z2RW` 

*Because...*
* SQL IDENTITY columns couple Id assignment with a database connection, creating a single point of failure, and restricting the ability to create object graphs in a disconnected operation.
* Guids / SQL UNIQUEIDENTIFIERs are terrible for clustered indexing, are not practically speakable, and look ugly.
* Sequential Guids / SQL SEQUENTIALIDs are extremely cumbersome to manage, aren't synchronized between app servers and database servers, nor in distributed environments. They also create tight coupling between application processes and the database server.


*Requirements*
* Ids must be ascending across a distributed environment
* Ids must not collide for the lifetime of the application, even in high-demand, distributed environments
* Ids must be assigned expecting case-insensitivity (SQL Server’s default collation)
* Ids should be of shorter length than Guids / UNIQUEIDENTIFIERs
* Dashes should be optional and not considered part of the Id

...your wish list here...

*Examples*
Ids are composed of some combination of a timestamp, a server hash[, a reserved character group], and a random component
* Guid: `{7331d71b-d1f1-443b-97f6-f24eeb207828}`
* Base36 [16]: `040VZ3C6SL3BZ2RW` or `040V-Z3C6-SL3B-Z2RW` 
** Structure: 10 + 2 + 1 + 3 (1 reserved character for implementer's purposes)
** Ascending over 115 year lifespan
** Less than 1300 possible hash combinations for server component
** ~46k hash combinations for random component
* Base36 [20]: `040VZ-C6SL0-1003B-Z00R2` = 11 + 5 + 0 + 5
** Structure: 11 + 4 + 0 + 5 (no reserved character)
** Ascending over 4,170 year lifespan
** 1.6 million possible hash combinations for server component
** 60 million possible hash combinations for random component
* Base36 [25]: `040VZ-C6SL0-1003B-Z00R2-01KR4` = 12 + 5 + 0 + 4
** Structure: 12 + 6 + 0 + 7 (no reserved character)
** Ascending over 150,000 year lifespan
** 2 billion possible hash combinationsfor server component
** 78 billion possible hash combinations for random component


