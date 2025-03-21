

$(document).ready(function () {
    // Evento para formatear el usuario id mientras el usuario escribe
    $("input[name='UsuarioID']").on("input", function () {
        let usuarioid = $(this).val().replace(/[^0-9]/g, ""); // Elimina caracteres no numéricos
        if (usuarioid.length > 1) usuarioid = usuarioid.slice(0, 1) + "-" + usuarioid.slice(1);
        if (usuarioid.length > 6) usuarioid = usuarioid.slice(0, 6) + "-" + usuarioid.slice(6);
        $(this).val(usuarioid); // Actualiza el campo con el formato correcto

        // Validación del formato  
        const cedulaPattern = /^\d-\d{4}-\d{4}$/;
        if (!cedulaPattern.test(usuarioid)) {
            $(this).addClass("is-invalid");
            $(".text-danger[for='UsuarioID']").text("El formato debe ser 2-0766-0660.");
        } else {
            $(this).removeClass("is-invalid");
            $(".text-danger[for='UsuarioID']").text("");
        }
    });
});


$(document).ready(function () {
    // Evento para formatear administrador id mientras el usuario escribe
    $("input[name='AdministradorID']").on("input", function () {
        let administradorid = $(this).val().replace(/[^0-9]/g, ""); // Elimina caracteres no numéricos
        if (administradorid.length > 1) administradorid = administradorid.slice(0, 1) + "-" + administradorid.slice(1);
        if (administradorid.length > 6) administradorid = administradorid.slice(0, 6) + "-" + administradorid.slice(6);
        $(this).val(administradorid); // Actualiza el campo con el formato correcto

        // Validación del formato
        const administradoridPattern = /^\d-\d{4}-\d{4}$/;
        if (!administradoridPattern.test(administradorid)) {
            $(this).addClass("is-invalid");
            $(".text-danger[for='AdministradorID']").text("El formato debe ser 2-0766-0660.");
        } else {
            $(this).removeClass("is-invalid");
            $(".text-danger[for='AdministradorID']").text("");
        }
    });
});



$(document).ready(function () {
    // Evento para formatear el proveedor id mientras el usuario escribe
    $("input[name='ProveedorID']").on("input", function () {
        let proveedorid = $(this).val().replace(/[^0-9]/g, ""); // Elimina caracteres no numéricos
        if (proveedorid.length > 1) proveedorid = proveedorid.slice(0, 1) + "-" + proveedorid.slice(1);
        if (proveedorid.length > 6) proveedorid = proveedorid.slice(0, 6) + "-" + proveedorid.slice(6);
        $(this).val(proveedorid); // Actualiza el campo con el formato correcto

        // Validación del formato  
        const cedulaPattern = /^\d-\d{4}-\d{4}$/;
        if (!cedulaPattern.test(proveedorid)) {
            $(this).addClass("is-invalid");
            $(".text-danger[for='ProveedorID']").text("El formato debe ser 2-0766-0660.");
        } else {
            $(this).removeClass("is-invalid");
            $(".text-danger[for='ProveedorID']").text("");
        }
    });
});

$(document).ready(function () {
    // Evento para formatear el productoid mientras el usuario escribe
    $("input[name='ProductoID']").on("input", function () {
        let productoid = $(this).val().replace(/[^0-9]/g, ""); // Elimina caracteres no numéricos
        if (productoid.length > 1) productoid = productoid.slice(0, 1) + "-" + productoid.slice(1);
        if (productoid.length > 6) productoid = productoid.slice(0, 6) + "-" + productoid.slice(6);
        $(this).val(productoid); // Actualiza el campo con el formato correcto

        // Validación del formato  
        const cedulaPattern = /^\d-\d{4}-\d{4}$/;
        if (!cedulaPattern.test(productoid)) {
            $(this).addClass("is-invalid");
            $(".text-danger[for='ProductoID']").text("El formato debe ser 2-0766-0660.");
        } else {
            $(this).removeClass("is-invalid");
            $(".text-danger[for='ProductoID']").text("");
        }
    });
});


$(document).ready(function () {
    $("#create-form input[name='Correo']").on("input", function () {
        const emailPattern = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.(com|net|org|edu|gov|xyz|info|co|io)$/;
        const correo = $(this).val();

        if (!emailPattern.test(correo)) {
            $(this).addClass("is-invalid");
            $(".text-danger[for='Correo']").text("El correo debe tener un formato válido (ejemplo: usuario@dominio.com).");
        } else {
            $(this).removeClass("is-invalid");
            $(".text-danger[for='Correo']").text("");
        }
    });
});

document.getElementById("FechaNacimiento").addEventListener("input", function (e) {
    const input = e.target;
    let value = input.value.replace(/[^0-9]/g, ""); // Elimina caracteres no numéricos
    if (value.length > 2) value = value.slice(0, 2) + "/" + value.slice(2);
    if (value.length > 5) value = value.slice(0, 5) + "/" + value.slice(5);
    input.value = value;
});




$(document).ready(function () {
    // Evento para formatear el número de teléfono mientras el usuario escribe
    $("input[name='Telefono']").on("input", function () {
        let telefono = $(this).val().replace(/[^0-9]/g, ""); // Elimina caracteres no numéricos

        // Inserta el guion automáticamente después del cuarto dígito
        if (telefono.length > 4) {
            telefono = telefono.slice(0, 4) + "-" + telefono.slice(4);
        }

        // Limita el número de caracteres al formato correcto
        if (telefono.length > 9) {
            telefono = telefono.slice(0, 9);
        }

        $(this).val(telefono); // Actualiza el campo con el formato correcto

        // Validación del formato
        const telefonoPattern = /^\d{4}-\d{4}$/;
        if (!telefonoPattern.test(telefono)) {
            $(this).addClass("is-invalid");
            $(".text-danger[for='Telefono']").text("El formato debe ser 8333-3838.");
        } else {
            $(this).removeClass("is-invalid");
            $(".text-danger[for='Telefono']").text("");
        }
    });
});
