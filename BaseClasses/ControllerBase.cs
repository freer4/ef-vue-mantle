using Microsoft.AspNetCore.Mvc;

namespace EfVueMantle;

[ApiController]
[Route("[controller]")]
public class ControllerBase<TModel, TService> : ControllerBase
    where TModel : ModelBase
    where TService : ServiceBase<TModel>
{

    private TService _service { get; set; }

    public ControllerBase(TService service)
    {
        _service = service;
    }


    [HttpGet("Get/{id}")]
    public virtual IActionResult Get(int id)
    {
        return Ok(_service.Get(id));
    }


    [HttpGet("All")]
    //Return all available records
    public virtual IActionResult Get()
    {
        List<TModel> list = _service.GetAll();
        return Ok(list);
    }


    //Return records from list of ids
    [HttpPost("List")]
    public virtual IActionResult GetList(List<int> ids)
    {
        List<TModel> list = _service.GetList(ids);
        return Ok(list);
    }

    //Return list of all available ids
    [HttpGet("AllIds")]
    public virtual IActionResult Series()
    {
        return Ok(_service.GetAllIds());
    }

    //Return list of ids based on query
    [HttpGet("Index/{type}/{prop}/{spec}")]
    public virtual IActionResult Index(string type, string prop, string spec)
    {
        List<int> list = new List<int>();
        try
        {
            switch (type)
            {
                case "order":
                    list = _service.Order(prop, Int32.Parse(spec));
                    break;
                case "equals":
                    list = _service.Equals(prop, spec);
                    break;
                case "contains":
                    list = _service.Contains(prop, spec);
                    break;
                default: throw new Exception($"Unknown Index \"{type}\" on prop {prop}");
            }
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        return Ok(list);
    }


    //Add instance of this model
    [HttpPost("Save")]
    public virtual IActionResult Save(TModel data)
    {
        _service.Save(data);
        return Ok(data);
    }

    [HttpDelete("Delete")]
    public virtual IActionResult Delete(int id)
    {
        _service.Delete(id);
        return Ok(true);
    }

}

