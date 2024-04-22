using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Snapshot
{
    public float simulationTime;
    public string speciesName;
    public float tPopulation;
    public float mHappiness;
    public float mCourage;
    public float mDesirability;

   public Snapshot(float _simulationTime, string _speciesName, float _tPopulation, float _mHappiness, float _mCourage, float _mDesirability)
   {
        simulationTime = _simulationTime;
        speciesName = _speciesName;
        tPopulation = _tPopulation;
        mHappiness = _mHappiness;
        mCourage = _mCourage;
        mDesirability = _mDesirability;
   }
}

public class DataExport {

    List<Snapshot> simulationData = new List<Snapshot>();

    public void AddSnapshot(float simulationTime, string speciesName, float tPopulation, float mHappiness, float mCourage, float mDesirability)
    {
        Snapshot newSnapshot = new Snapshot(simulationTime, speciesName, tPopulation, mHappiness, mCourage, mDesirability);
        simulationData.Add(newSnapshot);
    }

    public void Export()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "Data", "SimulationData.csv");
        // Ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

        using (StreamWriter sw = new StreamWriter(filePath))
        {
            // Write CSV header
            sw.WriteLine("SimulationTime,SpeciesName,TPopulation,MHappiness,MCourage,MDesirability");

            // Write each snapshot's data to the CSV file
            foreach (Snapshot snapshot in simulationData)
            {
                sw.WriteLine($"{snapshot.simulationTime},{snapshot.speciesName},{snapshot.tPopulation},{snapshot.mHappiness},{snapshot.mCourage},{snapshot.mDesirability}");
            }
        }

        Debug.Log("Simulation data exported to: " + filePath);
    }

}
