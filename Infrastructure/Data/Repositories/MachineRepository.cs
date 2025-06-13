using ManufacturingScheduler.Core.Interfaces;

namespace ManufacturingScheduler.Infrastructure.Data.Repositories
{
    using ManufacturingScheduler.Core.Models;
    public class MachineRepository : IMachineRepository
    {
        private readonly MockFileRepository<Machine> _mockRepository;

        public MachineRepository()
        {
            _mockRepository = new MockFileRepository<Machine>("machines.txt");
        }

        public async Task<List<Machine>> GetOperationalMachinesAsync()
        {
            var allMachines = await _mockRepository.GetAllAsync();
            return allMachines.Where(m => m.IsOperational).ToList();
        }

        public async Task<Machine?> GetMachineByIdAsync(int machineId)
        {
            return await _mockRepository.GetByIdAsync(machineId, (machine, id) => machine.Id == id);
        }
    }
}
