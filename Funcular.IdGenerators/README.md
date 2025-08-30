# Funcular.IdGenerators

A cross-process thread-safe C# utility to create ordered (but non-sequential), human speakable, case-insensitive, partially random (non-guessable) identifiers in Base36. Identifiers are composed of (in this order), a timestamp component, a server hash component, an optional number of reserved  characters, and a random component. Note: Source for the ExtensionMethods NuGet package dependency is available at *[Funcular.ExtensionMethods](https://github.com/piranout/Funcular.ExtensionMethods/ "Funcular Extension Methods")*. 

* Guid: `{7331d71b-d1f1-443b-97f6-f24eeb207828}`
* Base36 [20]: `040VKZ3C60SL3B1Z2RW5` or `040VK-Z3C60-SL3B1-Z2RW5`
* Dashes are cosmetic formatting, not part of the Id; store as a CHAR(20).
 
#### Usage
Create a generator instance by passing the lengths of the various components, plus any desired delimiter character and layout (optional), to the constructor. To generate Ids, simply call `NewId()` for a plain identifier or `NewId(true)` for a delimited one. The class is thread-safe, so your DI container can share a single instance across the entire app domain. See the Wiki for a complete multithreaded stress and performance test.

```csharp
var generator = new Base36IdGenerator(
                numTimestampCharacters: 12, 
                numServerCharacters: 6, 
                numRandomCharacters: 7, 
                reservedValue: "", 
                delimiter: "-", 
                delimiterPositions: new[] {20, 15, 10, 5})
Console.WriteLine(generator.NewId()); 
// "00E4WG2E7NMXEMFY919O2PIHS"
Console.WriteLine(generator.NewId(delimited: true));
// "00E4W-G2GTO-0IEMF-Y911Q-KJI8E"
```

#### Why? Because...
* SQL IDENTITY columns couple Id assignment with a database connection, creating a single point of failure, and restricting the ability to create object graphs in a disconnected operation.
* Guids / SQL UNIQUEIDENTIFIERs are terrible for clustered indexing, are not practically speakable, and look ugly.
* Sequential Guids / SQL SEQUENTIALIDs are extremely cumbersome to manage, aren't synchronized between app servers and database servers, nor in distributed environments. They also create tight coupling between application processes and the database server.
* This approach facilitates datastore-agnostic platforms, eases replication, and makes data significantly more portable

#### Requirements Met
* Ids must be ascending across a distributed environment
* Ids must not collide for the lifetime of the application, even in high-demand, distributed environments
* Ids must not be guessable; potential attackers should not be able to deduce any actual Ids using previous examples
* Ids must be assigned expecting case-insensitivity (SQL Server’s default collation)
* Ids should be of shorter length than Guids / UNIQUEIDENTIFIERs
* Dashes should be optional and not considered part of the Id

...your wish list here...

#### Examples
Ids are composed of some combination of a timestamp, a server hash, a reserved character group (optional), and a random component.
* Guid: `{7331d71b-d1f1-443b-97f6-f24eeb207828}`
* Base36 [16]: `040VZ3C6SL3BZ2RW` or `040V-Z3C6-SL3B-Z2RW` 
	* Structure: 10 + 2 + 1 + 3 (1 reserved character for implementer's purposes)
	* Ascending over 115 year lifespan
	* Less than 1300 possible hash combinations for server component
	* ~46k hash combinations for random component
* Base36 [20] (recommended): `040VZ-C6SL0-1003B-Z00R2`
	* Structure: 11 + 4 + 0 + 5 (no reserved character)
	* Ascending over 4,170 year lifespan
	* 1.6 million possible hash combinations for server component
	* 60 million possible hash combinations for random component
* Base36 [25]: `040VZ-C6SL0-1003B-Z00R2-01KR4`
	* Structure: 12 + 6 + 0 + 7 (no reserved character)
	* Ascending over 150,000 year lifespan
	* 2 billion possible hash combinations for server component
	* 78 billion possible hash combinations for random component


#### Testing
```csharp
[TestClass]
    public class IdGenerationTests
    {
        private Base36IdGenerator _idGenerator;

        [TestInitialize]
        public void Setup()
        {
            this._idGenerator = new Base36IdGenerator(
                numTimestampCharacters: 11,
                numServerCharacters: 5,
                numRandomCharacters: 4,
                reservedValue: "",
                delimiter: "-",
                // give the positions in reverse order if you
                // don't want to have to account for modifying
                // the loop internally. To do the same in ascending
                // order, you would need to pass 5, 11, 17 instead.
                delimiterPositions: new[] {15, 10, 5});
        }

        [TestMethod]
        public void TestIdsAreAscending()
        {
            string id1 = this._idGenerator.NewId();
            string id2 = this._idGenerator.NewId();
            Assert.IsTrue(String.Compare(id2, id1, StringComparison.OrdinalIgnoreCase) > 0);
        }

        [TestMethod]
        public void TestIdLengthsAreAsExpected()
        {
            // These are the segment lengths passed to the constructor:
            int expectedLength = 11 + 5 + 0 + 4;
            string id = this._idGenerator.NewId();
            Assert.AreEqual(id.Length, expectedLength);
            // Should include 3 delimiter dashes when called with (true):            
            id = this._idGenerator.NewId(true);
            Assert.AreEqual(id.Length, expectedLength + 3);
        }
```