using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaVotoModelos.DTOs;

public class LoginRequestDto
{
    public string Cedula { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}