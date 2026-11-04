-- Script de Creación de Base de Datos y Tablas
CREATE DATABASE VeterinariaDb;
GO
USE VeterinariaDb;
GO

CREATE TABLE Propietarios (
    IdPropietario INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    Dni VARCHAR(8) NOT NULL UNIQUE,
    Telefono VARCHAR(15) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE Mascotas (
    IdMascota INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(50) NOT NULL,
    Especie NVARCHAR(30) NOT NULL,
    Raza NVARCHAR(50) NOT NULL,
    Edad INT NOT NULL,
    Peso DECIMAL(5,2) NOT NULL,
    IdPropietario INT NOT NULL,
    CONSTRAINT FK_Mascotas_Propietarios FOREIGN KEY (IdPropietario) 
        REFERENCES Propietarios(IdPropietario) ON DELETE RESTRICT
);

-- Datos de Ejemplo
INSERT INTO Propietarios (Nombre, Dni, Telefono, Email) VALUES
('Carlos Mendoza', '71234567', '987654321', 'carlos.mendoza@email.com'),
('Ana Gutierrez', '87654321', '912345678', 'ana.gutierrez@email.com');

INSERT INTO Mascotas (Nombre, Especie, Raza, Edad, Peso, IdPropietario) VALUES
('Firulais', 'Perro', 'Golden Retriever', 3, 28.50, 1),
('Michi', 'Gato', 'Siames', 2, 4.20, 1),
('Rex', 'Perro', 'Pastor Aleman', 5, 32.00, 2);