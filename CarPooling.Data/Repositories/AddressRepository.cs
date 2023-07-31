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
    public class AddressRepository : IAddressRepository
    {
        private readonly CarPoolingDbContext _context;

        public AddressRepository(CarPoolingDbContext context)
        {
            _context = context;
        }

        public async Task<Address> CreateAsync(Address address)
        {
            _context.Addresses.Add(address);
            address.CreatedOn = DateTime.Now;
            await _context.SaveChangesAsync();
            return address;
        }

        public async Task<Address> DeleteAsync(int id)
        {
            Address addressToRemove = await GetByIdAsync(id);

            addressToRemove.IsDeleted = true;
            addressToRemove.DeletedOn = DateTime.Now;

            await _context.SaveChangesAsync();
            return addressToRemove;
        }

        public async Task<List<Address>> GetAllAsync()
        {
            return await _context.Addresses
                .Include(a => a.Details)
                .Include(a => a.City)
                .Include(a => a.Country)
                .ToListAsync();
        }

        public async Task<Address> GetByIdAsync(int id)
        {
            Address address = await _context.Addresses
                .Where(a => a.Id == id)
                //.Include(a => a.Details)
                //.Include(a => a.City)
                //.Include(a => a.Country)
                .FirstOrDefaultAsync();

            return address ?? throw new EntityNotFoundException($"Could not find an address with id: {id}!");
        }

        public async Task<Address> UpdateAsync(int id, Address address)
        {
            Address addressToUpdate = await GetByIdAsync(id);

            addressToUpdate.City = address.City;
            addressToUpdate.Country = address.Country;
            addressToUpdate.Details = address.Details;
            addressToUpdate.UpdatedOn = DateTime.Now;

            _context.Update(addressToUpdate);
            await _context.SaveChangesAsync();

            return addressToUpdate;
        }
    }
}
