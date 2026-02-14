//SECTION: Parameters
using UnityEngine;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.ComponentModel.DataAnnotations.Schema;

using CrossSection = System.Collections.Generic.Tuple<double, double>;
using System.Numerics; // energy (eV) to cross section (m^2) mapping

struct Collision
{
    public double energy; // excitation energy in eV
    public List<CrossSection> crossSections; // cross section as a function of energy
    public bool isIonization; // whether this collision results in ionization

    double GetCrossSection(double energy)
    {
        //interpolate cross section data to get cross section at given energy
        if (energy < crossSections[0].Item1 || energy > crossSections[crossSections.Count - 1].Item1)
        {
            return 0; // return 0 if energy is outside of range of cross section data
        }
        for (int i = 0; i < crossSections.Count - 1; i++)
        {
            if (energy >= crossSections[i].Item1 && energy <= crossSections[i + 1].Item1)
            {
                //linear interpolation
                double x0 = crossSections[i].Item1;
                double y0 = crossSections[i].Item2;
                double x1 = crossSections[i + 1].Item1;
                double y1 = crossSections[i + 1].Item2;
                return y0 + (y1 - y0) * (energy - x0) / (x1 - x0);
            }
        }
        return 0; // should never reach here
    }
}

struct Electron
{
    public Vector3 position; // position in m
    public Vector3 velocity; // velocity in m/s
    public double energy; // kinetic energy in eV
    public int order;
}

class PCG
{   
    public ulong state;
    const ulong multiplier = 6364136223846793005;
    public PCG(uint seed)
    {
        Init(seed);
    }

    public Init(uint seed)
    {
        state = 2 * seed + 1;
        Next();
    }

    uint Next()
    {
        ulong x = state;
        uint count = (uint)(x >> 61);
        state = x * multiplier;
        x ^= x >> 22;
        return (uint)(x >> (22 + count));
    }

    public RandomFloat()
    {
        return Next() / (double)uint.MaxValue;
    }

}

public class MonteCarloSim : MonoBehaviour
{
    // SECTION: Simulation parameters
    public int    maxCollisions = 10e6; // number of collisions to simulate for each electron
    public double reducedEfield = 100; // efield in z-direction in Td
    public double distance = 0.01; // distance between plates in m
    public double pressure = 10; // pressure of neon in Torr
    public double diameter = 0.05; // diameter of the simulation region in m
    // SECTION: Constants
    const double R = 8.314f; // ideal gas constant in J/(mol*K)
    const uint T = 300; // temperature in K
    const double torrToPa = 133.322f; // conversion factor from Torr to Pa
    const double avogadro = 6.022e23f; // Avogadro's number in mol^-1
    const double boltzmann = 1.381e-23f; // Boltzmann constant in J/K
    const double electronMass = 9.109e-31f; // mass of electron in kg
    const double eVtoJ = 1.602e-19f; // conversion factor from eV to J
    const double electronCharge = 1.602e-19f; // charge of electron in C
    // SECTION: Helper functions
    double GetDensity(double pressure)
    {
        //calculate number density of neon atoms in m^-3 using ideal gas law
        return (pressure * torrToPa * avogadro) / (R * T);
    }

    double GetEfield(double reducedEfield, double density)
    {
        //calculate electric field in V/m from reduced electric field in Td and number density in m^-3
        return reducedEfield * 1e-21f * density;
    }

    double EnergyToVelocity(double energy)
    {
        //convert energy in eV to velocity in m/s
        return Mathf.Sqrt(2 * energy * eVtoJ / electronMass);
    }

    const uint numEnergyBins = 1000; 
    
    double GetMaxFrequency()
    {
        float maxFrequency = 0;
        for (int i = 0; i < numEnergyBins; i++)
        {
            float energy = i * maxEnergy / numEnergyBins;
            double totalCrossSection = 0;
            foreach (Collision collision in collisions)
            {
                totalCrossSection += collision.GetCrossSection(energy);
            }
            double frequency = GetDensity(pressure) * totalCrossSection * EnergyToVelocity(energy); // calculate collision frequency using kinetic theory
            if (frequency > maxFrequency)
            {
                maxFrequency = frequency;
            }
        }
        return maxFrequency;
    }

    // SECTION: Collision data
    double maxEnergy = 0; // highest energy in cross section data
    List<Collision> collisions;
    void GetCollisionData()
    {
        //read collision data from cross section files
        FileStream fs = new("Assets/CData/cross_sections.txt", FileMode.Open);
        StreamReader sr = new(fs);
        collisions = [];
        while (!sr.EndOfStream)
        {
            //skip until line is ELASTIC, EXCITATION, or IONIZATION
            string line = sr.ReadLine();
            if (line.StartsWith("ELASTIC") || line.StartsWith("EXCITATION") || line.StartsWith("IONIZATION"))
            {
                Collision collision = new();
                collision.isIonization = line.StartsWith("IONIZATION");
                //skip until dashed line
                while (!line.StartsWith("-----"))
                {
                    line = sr.ReadLine();
                }
                //read next lines for cross section until we hit a dashed line
                collision.crossSections = [];
                while (!(line = sr.ReadLine()).StartsWith("-----"))
                {
                    string[] parts = line.Split('\t');
                    double energy = double.Parse(parts[0]);
                    if (energy > maxEnergy)
                    {
                        maxEnergy = energy;
                    }
                    double crossSection = double.Parse(parts[1]);
                    collision.crossSection.Add(new CrossSection(energy, crossSection));
                }
                collision.energy = collision.crossSections[0].Item1; // set energy to first energy value in cross section data
                collisions.Add(collision);
            }
        }
        return collisions;
    }

    // SECTION: Simulation
    PCG rng; 

    void ScatterElectron(Electron e)
    {
        //scatter electron isotropically
        double costheta = 1 - 2 * rng.RandomFloat(); // random cosine of polar angle
        double sintheta = Mathf.Sqrt(1 - costheta * costheta);
        double phi = rng.RandomFloat() * 2 * Mathf.PI; // random xy angle
        double velocityMagnitude = EnergyToVelocity(e.energy);
        e.velocity = new Vector3(velocityMagnitude * sintheta * Mathf.Cos(phi), velocityMagnitude * sintheta * Mathf.Sin(phi), velocityMagnitude * costheta); // set velocity vector
    }
    Electron GenStartingElectron()
    {
        Electron e = new();
        double r = rng.RandomFloat() * diameter / 2; // random radius from center of simulation region
        double theta = rng.RandomFloat() * 2 * Mathf.PI; // random angle in xy-plane
        e.position = new Vector3(r * Mathf.Cos(theta), r * Mathf.Sin(theta), 0); // set initial position at z=0
        e.energy = rng.randomFloat();
        ScatterElectron(e);
        if (e.velocity.z < 0)
        {
            e.velocity.z *= -1; // ensure electron is moving in positive z-direction
        }
        e.order = 0;
        return e;
    }

    List<Vector3> collisionPoints; // list to store collision points for visualization
    void runSimulation()
    {
        GetCollisionData();
        double maxFrequency = GetMaxFrequency();
        double density = GetDensity(pressure);
        double efield = GetEfield(reducedEfield, density);
        rng = new PCG(10); // theme!!!!
        List<Electron> electrons = [];
        collisionPoints = [];
        //initialize first electron at z=0, with random x,y position and maxwellian velocity distribution
        Electron e = GenStartingElectron();
        electrons.Add(e);
        uint collisionCount = 0;
        while (collisionCount < maxCollisions && electrons.Count > 0)
        {
            Electron currentElectron = electrons[0];
            electrons.RemoveAt(0);
            for (int i = 0; i < numCollisions; i++)
            {
                //find time of collision
                double dt = -1/maxFrequency * Mathf.Log(1 - rng.RandomFloat());
                //move electron for time dt
                currentElectron.position += currentElectron.velocity * dt;
                double acceleration = electronCharge * efield / electronMass; // acceleration in m/s^2
                currentElectron.position.z += 0.5 * acceleration * (dt * dt); // move in z-direction due to acceleration
                currentElectron.velocity += new Vector3(0, 0, acceleration); // accelerate in z-direction
                //check if electron has hit the plates
                if (currentElectron.position.z < 0 || currentElectron.position.z > distance || currentElectron.position.x * currentElectron.position.x + currentElectron.position.y * currentElectron.position.y > (diameter * diameter) / 4)
                {
                    break; // electron has hit the plates or exited the cylinder
                }
                e.energy = 0.5 * electronMass * currentElectron.velocity.sqrMagnitude / eVtoJ; // update energy in eV
                //determine if collision occurs
                float collisionNumber = rng.RandomFloat();
                float cumulativeProbability = 0;
                for (int j = 0; j < collisions.Count; j++)
                {
                    if (currentElectron.energy < collisions[j].energy)
                    {
                        continue; // skip collisions that require more energy than the electron has
                    } 
                    cumulativeProbability += density * collisions[j].GetCrossSection(currentElectron.energy) * EnergyToVelocity(currentElectron.energy) / maxFrequency; // calculate cumulative probability of collision
                    if (collisionNumber < cumulativeProbability)
                    {
                        collisionCount++;
                        collisionPoints.Add(currentElectron.position);
                        //collision occurs, determine type of collision
                        if (!collisions[j].isIonization)
                        {
                            //subtract excitation energy from electron energy
                            currentElectron.energy -= collisions[j].energy;
                            ScatterElectron(currentElectron);
                        } else
                        {
                            float energySplit = rng.RandomFloat() * (currentElectron.energy - collisions[j].energy); // random energy split between primary and secondary electron
                            currentElectron.energy = energySplit;
                            ScatterElectron(currentElectron);
                            Electron secondaryElectron = new()
                            {
                                position = currentElectron.position,
                                energy = currentElectron.energy - energySplit,
                                order = currentElectron.order + 1
                            };
                            ScatterElectron(secondaryElectron);
                            electrons.Add(secondaryElectron);
                        }
                        break;
                    }
                }
            }
        }
    }
}