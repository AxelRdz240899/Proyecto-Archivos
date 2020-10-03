using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
namespace Proyecto_Archivos
{
    class ManejoArbol
    {
        /*
         * Esta clase la utilizo para hacer el manejo eficaz de los arboles
        */
        Archivo ManejadorArchivo;
        FileStream archivo;
        public ManejoArbol()
        {
            ManejadorArchivo = new Archivo();
        }


        public void InsertaEnArbolPrimario(Atributo atributo, string Ruta, int Clave, long Direccion, string RutaArchivoDicc)
        {
            Arbol ArbolP = new Arbol(ManejadorArchivo.ObtenNodos(atributo, archivo, Ruta), atributo);
            InsercionArbol(ArbolP, Clave, Direccion, Ruta, atributo, RutaArchivoDicc);
        }

        public void InsertaEnArbolSecundario(Atributo atributo, string Ruta, int Clave, long Direccion, string RutaArchivoDicc)
        {
            Arbol ArbolS = new Arbol(ManejadorArchivo.ObtenNodos(atributo, archivo, Ruta), atributo);
            if (ArbolS.ContieneClaveEnHojas(Clave))
            {
                long DireccionBloque = ArbolS.ObtenDireccionDeHoja(Clave);
                List<long> BloqueLectura = ManejadorArchivo.LeeCajonIndiceSecundario(Ruta, archivo, DireccionBloque);

                MessageBox.Show("EL bloque tiene: " + BloqueLectura.Count);
                BloqueLectura.Add(Direccion);
                BloqueLectura.Sort();
                ManejadorArchivo.EscribeCajonSecundario(Ruta, archivo, DireccionBloque, BloqueLectura);
            }
            else
            {
                long DireccionBloque = ManejadorArchivo.ObtenUltimaDireccion(Ruta, archivo);
                ManejadorArchivo.EscribeCajonInicialIndiceSecundario(Ruta, archivo, DireccionBloque);
                InsercionArbol(ArbolS, Clave, DireccionBloque, Ruta, atributo, RutaArchivoDicc);
                List<long> Bloque = ManejadorArchivo.LeeCajonIndiceSecundario(Ruta, archivo, DireccionBloque);
                Bloque.Add(Direccion);
                Bloque.Sort();
                ManejadorArchivo.EscribeCajonSecundario(Ruta, archivo, DireccionBloque, Bloque);
            }
        }

        public void EliminaEnArbolSecundario(Atributo atributo, string Ruta, int Clave, string RutaArchivoDicc, long Direccion)
        {
            Arbol ArbolS = new Arbol(ManejadorArchivo.ObtenNodos(atributo, archivo, Ruta), atributo);
            Nodo Nodo = ArbolS.ObtenNodoConLaClave(Clave);
            long Dir = ArbolS.ObtenDireccionDeHoja(Clave);
            List<long> Bloque = ManejadorArchivo.LeeCajonIndiceSecundario(Ruta, archivo, Dir);
            Bloque.Remove(Direccion);
            if(Bloque.Count == 0)
            {
                EliminaEnArbol(ArbolS, Nodo, Clave, Direccion, Ruta, atributo, RutaArchivoDicc);
            }
            else
            {
                ManejadorArchivo.EscribeCajonSecundario(Ruta, archivo, Dir, Bloque);
            }
            if(ArbolS.Nodos.Count == 0)
            {
                ManejadorArchivo.CreaArchivo(Ruta, archivo);
            }
        }
        public void EliminaEnArbolPrimario(Atributo atributo, string Ruta, int Clave,string RutaArchivoDicc)
        {
            Arbol ArbolP = new Arbol(ManejadorArchivo.ObtenNodos(atributo, archivo, Ruta), atributo);
            Nodo NodoDondeSevaAEliminar = ArbolP.ObtenNodoConLaClave(Clave);
            long DireccionDondeEliminar = NodoDondeSevaAEliminar.ObtenApuntadorHoja(Clave);
            EliminaEnArbol(ArbolP, NodoDondeSevaAEliminar, Clave, DireccionDondeEliminar, Ruta, atributo, RutaArchivoDicc);
        }

        public bool EliminaEnArbol(Arbol arbol, Nodo nodo, int clave, long direccion, string Ruta, Atributo A, string RutaDicc)
        {
            int DatosMinimos = (Arbol.GradoArbol - 1) / 2;
            char tipo = nodo.TipoNodo;

            if (tipo == 'H')
            {
                if (!nodo.EliminaDatoEnHoja(clave))
                    return false;
            }
            else
            {
                if (!nodo.EliminaEnNodoRaiz(clave, direccion))
                    return false;
            }
            ManejadorArchivo.EscribeNodo(nodo, archivo, Ruta);
            if (tipo != 'R')
            {
                if (nodo.ObtenNumeroLlavesValidas() < DatosMinimos)
                {
                    Nodo padre = arbol.ObtenNodoPadre(nodo);
                    Nodo vecino_der = arbol.ObtenNodoVecinoDer(nodo);
                    Nodo vecino_izq = arbol.ObtenNodoVecinoIzq(nodo);

                    if (vecino_der != null && arbol.ChecaSiTienenElMismoPadre(nodo, vecino_der) && vecino_der.ObtenNumeroLlavesValidas() - 1 >= DatosMinimos)
                    {
                        if (tipo == 'H')
                        {
                            long prestado_dir = vecino_der.DireccionLlaves[0];
                            int prestado_cve = vecino_der.Llaves[0];

                            if (!vecino_der.EliminaDatoEnHoja(prestado_cve))
                                return false;
                            ManejadorArchivo.EscribeNodo(vecino_der, archivo, Ruta);

                            nodo.InsertaOrdenadoEnHoja(prestado_cve, prestado_dir);
                            ManejadorArchivo.EscribeNodo(nodo, archivo, Ruta);

                            int idx_actualizar_padre = padre.DireccionLlaves.IndexOf(nodo.DirNodo);
                            padre.Llaves[idx_actualizar_padre] = vecino_der.Llaves[0];
                            ManejadorArchivo.EscribeNodo(padre, archivo, Ruta);
                        }
                        else
                        {
                            long vecino_dir = vecino_der.DireccionLlaves[0];
                            int vecino_cve = vecino_der.Llaves[0];
                            int idx_cve_padre = padre.DireccionLlaves.IndexOf(nodo.DirNodo);
                            int padre_cve = padre.Llaves[idx_cve_padre];

                            if (!vecino_der.EliminaEnNodoRaiz(vecino_cve, vecino_dir))
                                return false;
                            ManejadorArchivo.EscribeNodo(vecino_der, archivo, Ruta);

                            padre.Llaves[idx_cve_padre] = vecino_cve;
                            ManejadorArchivo.EscribeNodo(padre, archivo, Ruta);

                            nodo.InsertaOrdenadoEnRaiz(padre_cve, vecino_dir);
                            ManejadorArchivo.EscribeNodo(nodo, archivo, Ruta);
                        }
                    }
                    else if (vecino_izq != null && arbol.ChecaSiTienenElMismoPadre(nodo, vecino_izq) && vecino_izq.ObtenNumeroLlavesValidas() - 1 >= DatosMinimos)
                    {
                        if (tipo == 'H')
                        {
                            long prestado_dir = vecino_izq.DireccionLlaves[vecino_izq.ObtenNumeroLlavesValidas() - 1];
                            int prestado_cve = vecino_izq.Llaves[vecino_izq.ObtenNumeroLlavesValidas() - 1];

                            if (!vecino_izq.EliminaDatoEnHoja(prestado_cve))
                                return false;
                            ManejadorArchivo.EscribeNodo(vecino_izq, archivo, Ruta);

                            nodo.InsertaOrdenadoEnHoja(prestado_cve, prestado_dir);
                            ManejadorArchivo.EscribeNodo(nodo, archivo, Ruta);

                            int idx_actualizar_padre = padre.DireccionLlaves.IndexOf(vecino_izq.DirNodo);
                            padre.Llaves[idx_actualizar_padre] = prestado_cve;
                            ManejadorArchivo.EscribeNodo(padre, archivo, Ruta);
                        }
                        else
                        {
                            long vecino_dir = vecino_izq.DireccionLlaves[vecino_izq.ObtenNumeroLlavesValidas()];
                            int vecino_cve = vecino_izq.Llaves[vecino_izq.ObtenNumeroLlavesValidas() - 1];
                            int idx_cve_padre = padre.DireccionLlaves.IndexOf(vecino_izq.DirNodo);
                            int padre_cve = padre.Llaves[idx_cve_padre];

                            if (!vecino_izq.EliminaEnNodoRaiz(vecino_cve, vecino_dir))
                                return false;
                            ManejadorArchivo.EscribeNodo(vecino_izq, archivo, Ruta);

                            padre.Llaves[idx_cve_padre] = vecino_cve;
                            ManejadorArchivo.EscribeNodo(padre, archivo, Ruta);

                            nodo.InsertaOrdenadoEnRaiz(padre_cve, vecino_dir);
                            ManejadorArchivo.EscribeNodo(nodo, archivo, Ruta);
                        }
                    }
                    else if (vecino_der != null && arbol.ChecaSiTienenElMismoPadre(nodo, vecino_der))
                    {
                        if (tipo == 'H')
                        {
                            for (int i = 0; i < vecino_der.ObtenNumeroLlavesValidas(); i++)
                                nodo.InsertaOrdenadoEnHoja(vecino_der.Llaves[i], vecino_der.DireccionLlaves[i]);
                            ManejadorArchivo.EscribeNodo(nodo, archivo, Ruta);
                            if (padre.TipoNodo == 'R' && padre.ObtenNumeroLlavesValidas() == 1)
                            {
                                A.Direccion_Indice = vecino_der.DirNodo;
                                ManejadorArchivo.ModificaAtributo(A, RutaDicc, archivo);
                            }
                            else
                            {
                                int idx_eliminar_padre = padre.DireccionLlaves.IndexOf(vecino_der.DirNodo);
                                int dato_nuevo = padre.Llaves[idx_eliminar_padre - 1];
                                long dir_nueva = padre.DireccionLlaves[idx_eliminar_padre];

                                return EliminaEnArbol(arbol, padre, dato_nuevo, dir_nueva,Ruta, A,RutaDicc);
                            }
                        }
                        else
                        {
                            int cve_padre = padre.Llaves[padre.DireccionLlaves.IndexOf(nodo.DirNodo)];
                            long dir0_vecino = vecino_der.DireccionLlaves[0];

                            vecino_der.InsertaOrdenadoEnRaiz(cve_padre, dir0_vecino);

                            for (int i = 0; i < nodo.ObtenNumeroLlavesValidas(); i++)
                                vecino_der.InsertaOrdenadoEnRaiz(nodo.Llaves[i], nodo.DireccionLlaves[i + 1]);
                            vecino_der.DireccionLlaves[0] = nodo.DireccionLlaves[0];

                            if (padre.TipoNodo == 'R' && padre.ObtenNumeroLlavesValidas() == 1)
                            {
                                vecino_der.TipoNodo = 'R';
                                ManejadorArchivo.EscribeNodo(vecino_der, archivo, Ruta);
                                A.Direccion_Indice = vecino_der.DirNodo;
                                ManejadorArchivo.ModificaAtributo(A, RutaDicc, archivo);
                            }
                            else
                            {
                                ManejadorArchivo.EscribeNodo(vecino_der,archivo, Ruta);
                                return EliminaEnArbol(arbol, padre, cve_padre, nodo.DirNodo, Ruta, A, RutaDicc);
                            }
                        }
                    }
                    else if (vecino_izq != null && arbol.ChecaSiTienenElMismoPadre(nodo, vecino_izq))
                    {
                        if (tipo == 'H')
                        {
                            for (int i = 0; i < nodo.ObtenNumeroLlavesValidas(); i++)
                                vecino_izq.InsertaOrdenadoEnHoja(nodo.Llaves[i], nodo.DireccionLlaves[i]);
                            ManejadorArchivo.EscribeNodo(vecino_izq,archivo, Ruta);
                            if (padre.TipoNodo == 'R' && padre.ObtenNumeroLlavesValidas() == 1)
                            {
                                vecino_izq.TipoNodo = 'R';
                                A.Direccion_Indice = vecino_izq.DirNodo;
                                ManejadorArchivo.ModificaAtributo(A, RutaDicc, archivo);
                            }
                            else
                            {
                                int idx_eliminar_padre = padre.DireccionLlaves.IndexOf(nodo.DirNodo);
                                int dato_nuevo = padre.Llaves[idx_eliminar_padre - 1];
                                long dir_nueva = padre.DireccionLlaves[idx_eliminar_padre];

                                return EliminaEnArbol(arbol, padre, dato_nuevo, dir_nueva, Ruta, A, RutaDicc);
                            }
                        }
                        else
                        {
                            int cve_padre = padre.Llaves[padre.DireccionLlaves.IndexOf(vecino_izq.DirNodo)];
                            long dir0_nodo = nodo.DireccionLlaves[0];

                            vecino_izq.InsertaOrdenadoEnRaiz(cve_padre, dir0_nodo);

                            for (int i = 0; i < nodo.ObtenNumeroLlavesValidas(); i++)
                                vecino_izq.InsertaOrdenadoEnRaiz(nodo.Llaves[i], nodo.DireccionLlaves[i + 1]);

                            if (padre.TipoNodo == 'R' && padre.ObtenNumeroLlavesValidas() == 1)
                            {
                                vecino_izq.TipoNodo = 'R';
                                ManejadorArchivo.EscribeNodo(vecino_izq,archivo, Ruta);
                                A.Direccion_Indice = vecino_izq.DirNodo;
                                ManejadorArchivo.ModificaAtributo(A, RutaDicc, archivo);
                            }
                            else
                            {
                                ManejadorArchivo.EscribeNodo(vecino_izq, archivo, Ruta);
                                return EliminaEnArbol(arbol, padre, cve_padre, nodo.DirNodo, Ruta, A, RutaDicc);
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void InsercionArbol(Arbol arbol, int Clave, long Direccion, string Ruta, Atributo atributo, string RutaArchDicc)
        {
            if (arbol.Nodos.Count == 0)
            {
                Nodo NuevaHoja = CreaNodo('H', ManejadorArchivo.ObtenUltimaDireccion(Ruta, archivo));
                NuevaHoja.Llaves[0] = Clave;
                NuevaHoja.DireccionLlaves[0] = Direccion;
                ManejadorArchivo.EscribeNodo(NuevaHoja, archivo, Ruta);
                atributo.Direccion_Indice = NuevaHoja.DirNodo;
                ManejadorArchivo.ModificaAtributo(atributo, RutaArchDicc, archivo);
            }
            else if (!arbol.TieneRaiz()) // En caso de que no tenga Raiz(Solamente una hoja)
            {
                Nodo Hoja = arbol.Nodos[0];
                List<Nodo> nodos = InsertaEnHoja(Hoja, Ruta, Clave, Direccion);
                if (nodos.Count == 2)
                {
                    Nodo NuevaRaiz = CreaNodo('R', ManejadorArchivo.ObtenUltimaDireccion(Ruta, archivo));
                    NuevaRaiz.DireccionLlaves[0] = nodos[0].DirNodo;
                    NuevaRaiz.DireccionLlaves[1] = nodos[1].DirNodo;
                    NuevaRaiz.Llaves[0] = nodos[1].Llaves[0];
                    atributo.Direccion_Indice = NuevaRaiz.DirNodo;
                    ManejadorArchivo.EscribeNodo(NuevaRaiz, archivo, Ruta);
                    ManejadorArchivo.ModificaAtributo(atributo, RutaArchDicc, archivo);
                }
            }
            else
            {
                Nodo Padre;
                Nodo Hijo = arbol.ObtenRaiz();

                do
                {
                    Padre = Hijo;
                    int i = Padre.ObtenIndiceClave(Clave);
                    Hijo = arbol.GetNodo(Padre.DireccionLlaves[i]);
                } while (Hijo.TipoNodo != 'H');
                List<Nodo> Res = InsertaEnHoja(Hijo, Ruta, Clave, Direccion);
                if (Res.Count == 2)
                {
                    ActualizaNodoPadre(Padre, Res[1].Llaves[0],Res[1].DirNodo , arbol, Ruta, RutaArchDicc,atributo);
                }
            }
        }

        public List<Nodo> InsertaEnHoja(Nodo Hoja, string Ruta, int Clave, long Direccion)
        {
            List<Nodo> Nodos = new List<Nodo>();
            if (!Hoja.InsertaOrdenadoEnHoja(Clave, Direccion)) // En caso de que ya no haya espacio en la hoja
            {
                //MessageBox.Show("Estoy dividiendo la hoja");
                List<Nodo> NodosDivision = DivideHoja(Hoja, Clave, Direccion, Ruta);
                ManejadorArchivo.EscribeNodo(NodosDivision[0], archivo, Ruta);
                ManejadorArchivo.EscribeNodo(NodosDivision[1], archivo, Ruta);
                return NodosDivision;
            }
            else
            {
                ManejadorArchivo.EscribeNodo(Hoja, archivo, Ruta);
                Nodos.Add(Hoja);
                return Nodos;
            }
        }

        public List<Nodo> DivideHoja(Nodo Nodo, int Clave, long Direccion, string Ruta)
        {
            Nodo Nuevo = CreaNodo('H', ManejadorArchivo.ObtenUltimaDireccion(Ruta, archivo));
            int iDivisor = (Arbol.GradoArbol - 1) / 2 - 1;
            List<long> AuxLong = new List<long>();
            List<int> AuxInt = new List<int>();
            foreach (var item in Nodo.DireccionLlaves)
                AuxLong.Add(item);

            foreach (var item in Nodo.Llaves)
                AuxInt.Add(item);

            AuxInt.Add(Clave);
            AuxInt.Sort();

            Nodo N1 = new Nodo();
            N1.TipoNodo = Nodo.TipoNodo;
            N1.DirNodo = Nodo.DirNodo;
            N1.DireccionLlaves[Arbol.GradoArbol - 1] = Nodo.DireccionLlaves[Arbol.GradoArbol - 1];

            int AuxViejo = 0, AuxNuevo = 0, AuxTemp = 0;
            for (int i = 0; i < Arbol.GradoArbol; i++)
            {
                if (i <= iDivisor)
                {
                    if (AuxInt[i] == Clave)
                        N1.DireccionLlaves[AuxViejo] = Direccion;
                    else
                        N1.DireccionLlaves[AuxViejo] = AuxLong[AuxTemp++];
                    N1.Llaves[AuxViejo++] = AuxInt[i];
                }
                else
                {
                    if (AuxInt[i] == Clave)
                        Nuevo.DireccionLlaves[AuxNuevo] = Direccion;
                    else
                        Nuevo.DireccionLlaves[AuxNuevo] = AuxLong[AuxTemp++];
                    Nuevo.Llaves[AuxNuevo++] = AuxInt[i];
                }
            }

            Nuevo.DireccionLlaves[Arbol.GradoArbol - 1] = N1.DireccionLlaves[Arbol.GradoArbol - 1];
            N1.DireccionLlaves[Arbol.GradoArbol - 1] = Nuevo.DirNodo;

            return new List<Nodo> { N1, Nuevo };
        }

        public Nodo CreaNodo(char Tipo, long Direccion)
        {
            Nodo Nuevo = new Nodo();
            Nuevo.TipoNodo = Tipo;
            Nuevo.DirNodo = Direccion;
            return Nuevo;
        }

        public void ActualizaNodoPadre(Nodo Padre, int Clave, long Direccion, Arbol a, string Ruta, string RutaArchDicc, Atributo A)
        {
            if(Padre.InsertaOrdenadoEnRaiz(Clave, Direccion)) // En caso de que el padre todavía tenga espacio
            {
                ManejadorArchivo.EscribeNodo(Padre, archivo, Ruta);
            }
            else
            {
                char TipoNodo = Padre.TipoNodo;
                if(TipoNodo == 'R') // El padre se vuelve intermedio
                {
                    Padre.TipoNodo = 'I';
                }
                List<int> AuxInt = new List<int>();
                foreach(int i in Padre.Llaves)
                {
                    AuxInt.Add(i);
                }
                AuxInt.Add(Clave); // Se inserta la primera clave del nuevo nodo
                AuxInt.Sort();
                int IndiceDivisorio = (Arbol.GradoArbol - 1) / 2;
                int ClaveASubir = AuxInt[IndiceDivisorio];
                List<Nodo> Intermedios = DivideRaiz(Clave, Direccion, AuxInt, Padre, Ruta);

                ManejadorArchivo.EscribeNodo(Intermedios[0], archivo, Ruta);
                ManejadorArchivo.EscribeNodo(Intermedios[1], archivo, Ruta);

                if(TipoNodo == 'R') // En caso de que el nodo que se dividió era raiz
                {
                    Nodo NuevaRaiz = CreaNodo('R', ManejadorArchivo.ObtenUltimaDireccion(Ruta, archivo));
                    NuevaRaiz.DireccionLlaves[0] = Intermedios[0].DirNodo;
                    NuevaRaiz.DireccionLlaves[1] = Intermedios[1].DirNodo;
                    NuevaRaiz.Llaves[0] = ClaveASubir;
                    A.Direccion_Indice = NuevaRaiz.DirNodo;
                    ManejadorArchivo.ModificaAtributo(A, RutaArchDicc, archivo);
                    ManejadorArchivo.EscribeNodo(NuevaRaiz, archivo, Ruta);
                }
                else
                {
                    Nodo PadreTemp = a.ObtenNodoPadre(Intermedios[0]);
                    ActualizaNodoPadre(PadreTemp, ClaveASubir, Intermedios[1].DirNodo,
                        new Arbol(ManejadorArchivo.ObtenNodos(A, archivo, Ruta),A), Ruta, RutaArchDicc, A);
                }
            }
        }

        /*public bool EliminaEnArbol()
        { /*
            int tam_minimo = (Arbol.GradoArbol - 1) / 2;
            char tipo = nodo.tipo;

            if (tipo == 'H')
            {
                if (!nodo.EliminaEnHoja(dato))
                    return false;
            }
            else
            {
                if (!nodo.EliminaEnNodoDenso(dato, direccion))
                    return false;
            }

            EscribeNodoArbolEnArchivo(arbol.atributo, nodo);

            if (tipo != 'R')
            {
                if (nodo.CountClaves() < tam_minimo)
                {
                    NodoArbol padre = arbol.GetPadre(nodo);
                    NodoArbol vecino_der = arbol.GetVecinoDer(nodo);
                    NodoArbol vecino_izq = arbol.GetVecinoIzq(nodo);

                    if (vecino_der != null && arbol.CheckMismoPadre(nodo, vecino_der) && vecino_der.CountClaves() - 1 >= tam_minimo)
                    {
                        if (tipo == 'H')
                        {
                            long prestado_dir = vecino_der.apuntadores[0];
                            int prestado_cve = vecino_der.claves[0];

                            if (!vecino_der.EliminaEnHoja(prestado_cve))
                                return false;
                            EscribeNodoArbolEnArchivo(arbol.atributo, vecino_der);

                            nodo.InsertaEnHoja(prestado_cve, prestado_dir);
                            EscribeNodoArbolEnArchivo(arbol.atributo, nodo);

                            int idx_actualizar_padre = padre.apuntadores.IndexOf(nodo.direccion);
                            padre.claves[idx_actualizar_padre] = vecino_der.claves[0];
                            EscribeNodoArbolEnArchivo(arbol.atributo, padre);
                        }
                        else
                        {
                            long vecino_dir = vecino_der.apuntadores[0];
                            int vecino_cve = vecino_der.claves[0];
                            int idx_cve_padre = padre.apuntadores.IndexOf(nodo.direccion);
                            int padre_cve = padre.claves[idx_cve_padre];

                            if (!vecino_der.EliminaEnNodoDenso(vecino_cve, vecino_dir))
                                return false;
                            EscribeNodoArbolEnArchivo(arbol.atributo, vecino_der);

                            padre.claves[idx_cve_padre] = vecino_cve;
                            EscribeNodoArbolEnArchivo(arbol.atributo, padre);

                            nodo.InsertaEnNodoDenso(padre_cve, vecino_dir);
                            EscribeNodoArbolEnArchivo(arbol.atributo, nodo);
                        }
                    }
                    else if (vecino_izq != null && arbol.CheckMismoPadre(nodo, vecino_izq) && vecino_izq.CountClaves() - 1 >= tam_minimo)
                    {
                        if (tipo == 'H')
                        {
                            long prestado_dir = vecino_izq.apuntadores[vecino_izq.CountClaves() - 1];
                            int prestado_cve = vecino_izq.claves[vecino_izq.CountClaves() - 1];

                            if (!vecino_izq.EliminaEnHoja(prestado_cve))
                                return false;
                            EscribeNodoArbolEnArchivo(arbol.atributo, vecino_izq);

                            nodo.InsertaEnHoja(prestado_cve, prestado_dir);
                            EscribeNodoArbolEnArchivo(arbol.atributo, nodo);

                            int idx_actualizar_padre = padre.apuntadores.IndexOf(vecino_izq.direccion);
                            padre.claves[idx_actualizar_padre] = prestado_cve;
                            EscribeNodoArbolEnArchivo(arbol.atributo, padre);
                        }
                        else
                        {
                            long vecino_dir = vecino_izq.apuntadores[vecino_izq.CountClaves()];
                            int vecino_cve = vecino_izq.claves[vecino_izq.CountClaves() - 1];
                            int idx_cve_padre = padre.apuntadores.IndexOf(vecino_izq.direccion);
                            int padre_cve = padre.claves[idx_cve_padre];

                            if (!vecino_izq.EliminaEnNodoDenso(vecino_cve, vecino_dir))
                                return false;
                            EscribeNodoArbolEnArchivo(arbol.atributo, vecino_izq);

                            padre.claves[idx_cve_padre] = vecino_cve;
                            EscribeNodoArbolEnArchivo(arbol.atributo, padre);

                            nodo.InsertaEnNodoDenso(padre_cve, vecino_dir);
                            EscribeNodoArbolEnArchivo(arbol.atributo, nodo);
                        }
                    }
                    else if (vecino_der != null && arbol.CheckMismoPadre(nodo, vecino_der))
                    {
                        if (tipo == 'H')
                        {
                            for (int i = 0; i < vecino_der.CountClaves(); i++)
                                nodo.InsertaEnHoja(vecino_der.claves[i], vecino_der.apuntadores[i]);
                            EscribeNodoArbolEnArchivo(arbol.atributo, nodo);
                            if (padre.tipo == 'R' && padre.CountClaves() == 1)
                            {
                                EscribeCabeceraIndice(arbol.atributo.direccion, vecino_der.direccion);
                            }
                            else
                            {
                                int idx_eliminar_padre = padre.apuntadores.IndexOf(vecino_der.direccion);
                                int dato_nuevo = padre.claves[idx_eliminar_padre - 1];
                                long dir_nueva = padre.apuntadores[idx_eliminar_padre];

                                return EliminaDeArbol(arbol, padre, dato_nuevo, dir_nueva);
                            }
                        }
                        else
                        {
                            int cve_padre = padre.claves[padre.apuntadores.IndexOf(nodo.direccion)];
                            long dir0_vecino = vecino_der.apuntadores[0];

                            vecino_der.InsertaEnNodoDenso(cve_padre, dir0_vecino);

                            for (int i = 0; i < nodo.CountClaves(); i++)
                                vecino_der.InsertaEnNodoDenso(nodo.claves[i], nodo.apuntadores[i + 1]);
                            vecino_der.apuntadores[0] = nodo.apuntadores[0];

                            if (padre.tipo == 'R' && padre.CountClaves() == 1)
                            {
                                vecino_der.tipo = 'R';
                                EscribeNodoArbolEnArchivo(arbol.atributo, vecino_der);
                                EscribeCabeceraIndice(arbol.atributo.direccion, vecino_der.direccion);
                            }
                            else
                            {
                                EscribeNodoArbolEnArchivo(arbol.atributo, vecino_der);
                                return EliminaDeArbol(arbol, padre, cve_padre, nodo.direccion);
                            }
                        }
                    }
                    else if (vecino_izq != null && arbol.CheckMismoPadre(nodo, vecino_izq))
                    {
                        if (tipo == 'H')
                        {
                            for (int i = 0; i < nodo.CountClaves(); i++)
                                vecino_izq.InsertaEnHoja(nodo.claves[i], nodo.apuntadores[i]);
                            EscribeNodoArbolEnArchivo(arbol.atributo, vecino_izq);
                            if (padre.tipo == 'R' && padre.CountClaves() == 1)
                            {
                                vecino_izq.tipo = 'R';
                                EscribeCabeceraIndice(arbol.atributo.direccion, vecino_izq.direccion);
                            }
                            else
                            {
                                int idx_eliminar_padre = padre.apuntadores.IndexOf(nodo.direccion);
                                int dato_nuevo = padre.claves[idx_eliminar_padre - 1];
                                long dir_nueva = padre.apuntadores[idx_eliminar_padre];

                                return EliminaDeArbol(arbol, padre, dato_nuevo, dir_nueva);
                            }
                        }
                        else
                        {
                            int cve_padre = padre.claves[padre.apuntadores.IndexOf(vecino_izq.direccion)];
                            long dir0_nodo = nodo.apuntadores[0];

                            vecino_izq.InsertaEnNodoDenso(cve_padre, dir0_nodo);

                            for (int i = 0; i < nodo.CountClaves(); i++)
                                vecino_izq.InsertaEnNodoDenso(nodo.claves[i], nodo.apuntadores[i + 1]);

                            if (padre.tipo == 'R' && padre.CountClaves() == 1)
                            {
                                vecino_izq.tipo = 'R';
                                EscribeNodoArbolEnArchivo(arbol.atributo, vecino_izq);
                                EscribeCabeceraIndice(arbol.atributo.direccion, vecino_izq.direccion);
                            }
                            else
                            {
                                EscribeNodoArbolEnArchivo(arbol.atributo, vecino_izq);
                                return EliminaDeArbol(arbol, padre, cve_padre, nodo.direccion);
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }*/
        public List<Nodo> DivideRaiz(int Clave, long Direccion, List<int> ClavesOrdenadas, Nodo nodo, string Ruta)
        {
            Nodo Nuevo = CreaNodo(nodo.TipoNodo, ManejadorArchivo.ObtenUltimaDireccion(Ruta, archivo));
            int IndiceDivisor = (Arbol.GradoArbol - 1) / 2;
            Nodo N1 = new Nodo();
            List<long> AuxLong = new List<long>();

            foreach(long l in nodo.DireccionLlaves)
            {
                AuxLong.Add(l);
            }
            N1.TipoNodo = nodo.TipoNodo;
            N1.DirNodo = nodo.DirNodo;

            int IndiceValorCentral = nodo.Llaves.IndexOf(ClavesOrdenadas[IndiceDivisor]) + 1; // Aquí obtenemos el dato que vamos a subir

            N1.DireccionLlaves[0] = AuxLong[0];
            Nuevo.DireccionLlaves[0] = AuxLong[IndiceValorCentral];

            AuxLong.RemoveAt(IndiceValorCentral);
            AuxLong.RemoveAt(0);
            int AuxViejo = 0, AuxNuevo = 0, AuxTemp = 0;
            for (int i = 0; i < Arbol.GradoArbol; i++)
            {
                if (i < IndiceDivisor)
                {
                    if (ClavesOrdenadas[i] == Clave)
                        N1.DireccionLlaves[AuxViejo + 1] = Direccion;
                    else
                        N1.DireccionLlaves[AuxViejo + 1] = AuxLong[AuxTemp++];
                    N1.Llaves[AuxViejo++] = ClavesOrdenadas[i];
                }
                else if(i > IndiceDivisor)
                {
                    if (ClavesOrdenadas[i] == Clave)
                        Nuevo.DireccionLlaves[AuxNuevo + 1] = Direccion;
                    else
                        Nuevo.DireccionLlaves[AuxNuevo + 1] = AuxLong[AuxTemp++];
                    Nuevo.Llaves[AuxNuevo++] = ClavesOrdenadas[i];
                }
            }
            return new List<Nodo> { N1, Nuevo };
        }
    }
}
