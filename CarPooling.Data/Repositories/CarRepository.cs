﻿using CarPooling.Data.Data;
using CarPooling.Data.Exceptions;
using CarPooling.Data.Models;
using CarPooling.Data.Repositories.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarPooling.Data.Repositories
{
    public class CarRepository : ICarRepository
    {
        private readonly CarPoolingDbContext _context;

        public CarRepository(CarPoolingDbContext context)
        {
            _context = context;
        }

        public async Task<Car> CreateAsync(Car car)
        {
            if (await _context.Cars.AnyAsync(c => c.Registration.Equals(car.Registration)))
            {
                throw new DuplicateEntityException("Such car already exists!");
            }
            car.CreatedOn = DateTime.Now;
            _context.Cars.Add(car);
            await _context.SaveChangesAsync();
            return car;
        }

        public async Task<Car> DeleteAsync(int id)
        {
            Car carToDelete = await GetByIdAsync(id);

            carToDelete.IsDeleted = true;
            carToDelete.DeletedOn = DateTime.Now;

            await _context.SaveChangesAsync();
            return carToDelete;
        }

        public async Task<List<Car>> FilterCarsAndSortAsync(string sortBy)
        {
            IQueryable<Car> cars = _context.Cars
                .Where(c => c.IsDeleted == false)
                .Include(c => c.Brand)
                .Include(c => c.Model)
                .Include(c => c.Registration)
                .Include(c => c.Driver)
                .Include(c => c.CreatedOn)
                .Include(c => c.UpdatedOn)
                .Include(c => c.DeletedOn);

            switch (sortBy)
            {
                case "create":
                    cars = cars.OrderBy(c => c.CreatedOn);
                    break;
                case "brand":
                    cars = cars.OrderBy(cars => cars.Brand);
                    break;
                case "model":
                    cars = cars.OrderBy(cars => cars.Model);
                    break;
                default:
                    cars = cars.OrderBy(cars => cars.Id);
                    break;
            }
            return await cars.ToListAsync();
        }

        public async Task<List<Car>> GetAllAsync()
        {
            return await _context.Cars
                .Where(c => c.IsDeleted == false)
                .Include(c => c.Brand)
                .Include(c => c.Model)
                .Include(c => c.Registration)
                .Include(c => c.Driver)
                .Include(c => c.CreatedOn)
                .Include(c => c.UpdatedOn)
                .Include(c => c.DeletedOn)
                .ToListAsync();
        }

        public async Task<Car> GetByBrandAsync(string brandName)
        {
            Car car = await _context.Cars
                .Where(c => c.IsDeleted == false)
                .Include(c => c.Id)
                .Include(c => c.Model)
                .Include(c => c.Registration)
                .Include(c => c.Driver)
                .Include(c => c.CreatedOn)
                .Include(c => c.UpdatedOn)
                .Include(c => c.DeletedOn)
                .FirstOrDefaultAsync(c => c.Brand.Equals(brandName));

            return car ?? throw new EntityNotFoundException("Not existing car with such brand!");
        }

        public async Task<Car> GetByIdAsync(int id)
        {
            Car car = await _context.Cars
                .Where(c => c.IsDeleted == false)
                .Include(c => c.Brand)
                .Include(c => c.Model)
                .Include(c => c.Registration)
                .Include(c => c.Driver)
                .Include(c => c.CreatedOn)
                .Include(c => c.UpdatedOn)
                .Include(c => c.DeletedOn)
                .FirstOrDefaultAsync(c => c.Id == id);

            return car ?? throw new EntityNotFoundException("There is no such car!");
        }

        public async Task<Car> UpdateAsync(int id, Car car)
        {
            Car carToUpdate = await GetByIdAsync(id);

            carToUpdate.TotalSeats = car.TotalSeats;
            carToUpdate.AvailableSeats = car.AvailableSeats;
            carToUpdate.Color = car.Color;
            carToUpdate.Registration = car.Registration;
            carToUpdate.CanSmoke = car.CanSmoke;
            carToUpdate.Brand = car.Brand;
            carToUpdate.Model = car.Model;
            carToUpdate.UpdatedOn = car.UpdatedOn;

            _context.Update(carToUpdate);
            await _context.SaveChangesAsync();

            return carToUpdate;
        }
    }
}