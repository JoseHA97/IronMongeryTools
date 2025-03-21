IronMongeryTools

Este es un sistema de gestión de inventario y ventas desarrollado con fines educativos. Está diseñado para administrar productos, proveedores y permitir a los usuarios realizar compras de manera eficiente.

Se puede registrar como usuario o administrador. El administrador tiene acceso a todo el sistema, mientras que los usuarios solo pueden gestionar su propio perfil y acceder a la página de compras.

El sistema está desarrollado con una arquitectura MVC, implementa CRUD, y ha sido creado en Visual Studio Code utilizando C#, JavaScript, HTML y CSS. Como gestor de base de datos, se ha utilizado Microsoft SQL Server Management Studio.

¿Como funciona?

Se realiza el registro en el sistema como administrador y se debe confirmar el correo para obtener acceso. Luego, se agregan los proveedores con toda la información relevante junto con los productos que ofrecen. Posteriormente, en la página de productos, se registran los productos que se desean ofrecer, asociándolos con el proveedor ya inscrito e incluyendo detalles como nombre, precio y stock, entre otros. Todo este procedimiento permite contar con un catálogo completo de productos en la página de compras, donde los usuarios pueden agregarlos a su carrito.

Como usuario, el proceso inicia con el registro y la confirmación de cuenta. Una vez dentro del sistema, se puede acceder a la pestaña de compras para agregar o quitar productos del carrito. También es posible ingresar al perfil de usuario, donde está pendiente la implementación de un historial de compras y la ampliación de funcionalidades.

Cuando se realiza una compra, esta queda registrada en el módulo de ventas, en los detalles de ventas y en los movimientos de inventario para su posterior consulta. Si el stock de un producto llega a cero, el sistema mostrará un mensaje al usuario informando la falta de existencias.

El sistema, al ser CRUD, permite crear, editar, visualizar y eliminar registros en la mayoría de sus módulos. Sin embargo, en secciones como ventas, detalles de ventas y movimientos de inventario, algunas opciones de eliminación serán restringidas. Estas mejoras, junto con la incorporación de nuevas funcionalidades, se implementarán gradualmente.
