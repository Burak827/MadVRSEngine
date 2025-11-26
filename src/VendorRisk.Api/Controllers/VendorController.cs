using Microsoft.AspNetCore.Mvc;
using VendorRisk.Domain.Interfaces;
using VendorRisk.Domain.Models;

namespace VendorRisk.Api.Controllers;

[ApiController]
[Route("api/vendors")]
[Route("api/vendor")]
public class VendorController : ControllerBase
{
    private readonly IVendorProfileRepository _repository;
    private readonly IRiskEngine _riskEngine;

    public VendorController(IVendorProfileRepository repository, IRiskEngine riskEngine)
    {
        _repository = repository;
        _riskEngine = riskEngine;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<VendorProfile>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var vendors = await _repository.ListAsync(cancellationToken);
        return Ok(vendors);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(VendorProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var vendor = await _repository.GetAsync(id, cancellationToken);
        if (vendor is null)
        {
            return NotFound();
        }

        return Ok(vendor);
    }

    [HttpPost]
    [ProducesResponseType(typeof(VendorProfile), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] VendorProfile vendor, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        vendor.SecurityCerts ??= new List<string>();
        vendor.Documents ??= new VendorDocuments();

        var created = await _repository.AddAsync(vendor, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] VendorProfile vendor, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        vendor.Id = id;
        vendor.SecurityCerts ??= new List<string>();
        vendor.Documents ??= new VendorDocuments();

        await _repository.UpdateAsync(vendor, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        await _repository.DeleteAsync(existing, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:int}/risk")]
    [ProducesResponseType(typeof(RiskAssessment), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRisk(int id, CancellationToken cancellationToken)
    {
        var vendor = await _repository.GetAsync(id, cancellationToken);
        if (vendor is null)
        {
            return NotFound();
        }

        var assessment = await _riskEngine.EvaluateAsync(vendor, cancellationToken);
        return Ok(assessment);
    }
}
